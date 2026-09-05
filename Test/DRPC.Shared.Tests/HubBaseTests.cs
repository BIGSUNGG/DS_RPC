using Communication.Network.RUDP;
using DRPC.Shared;
using DRPC.Shared.Message;
using DRPC.Shared.Network;
using Xunit;

namespace DRPC.Shared.Tests;

/// <summary>
/// HubBase 단위 테스트: CallId·타임아웃·동시성·오류 경로·전송 방식 배선. 네트워크 없이 FakeSession 으로 본다.
/// </summary>
public class HubBaseTests
{
    static readonly byte[] Payload = { 1, 2, 3 };

    [Fact]
    public async Task SendRPC_OneWay_UsesCallIdZero()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        await hub.SendRPC(7, Payload, RpcDeliveryMode.ReliableOrdered);

        var request = First<ProcedureCallRequestMessage>(session);
        Assert.Equal(0u, request.CallId);
        Assert.Equal(7, request.MethodId);
        Assert.Equal(Payload, request.ParameterData);
    }

    [Fact]
    public async Task SendRPC_AppliesRequestedDeliveryMode()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        await hub.SendRPC(1, Payload, RpcDeliveryMode.Unreliable);
        await hub.SendRPC(2, Payload, RpcDeliveryMode.ReliableSequenced);

        Assert.Equal(RudpDeliveryMethod.Unreliable, Assert.IsType<RudpSendOptions>(session.SentOptions[0]).DeliveryMethod);
        Assert.Equal(RudpDeliveryMethod.ReliableSequenced, Assert.IsType<RudpSendOptions>(session.SentOptions[1]).DeliveryMethod);
    }

    [Fact]
    public async Task RequestRPC_AllocatesNonZeroMonotonicCallIds()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        Task<byte[]> first = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);
        Task<byte[]> second = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);

        var sent = session.Sent.OfType<ProcedureCallRequestMessage>().ToArray();
        Assert.Equal(2, sent.Length);
        Assert.Equal(1u, sent[0].CallId);
        Assert.Equal(2u, sent[1].CallId);

        hub.OnReceiveRPCResponseMessage(new ProcedureCallResponseMessage(1u, Payload));
        hub.OnReceiveRPCResponseMessage(new ProcedureCallResponseMessage(2u, Payload));
        Assert.Equal(Payload, await first);
        Assert.Equal(Payload, await second);
    }

    [Fact]
    public async Task RequestRPC_Timeout_ThrowsTimeoutException()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session) { RpcTimeout = TimeSpan.FromMilliseconds(50) };

        await Assert.ThrowsAsync<TimeoutException>(() => hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered));
    }

    [Fact]
    public async Task RequestRPC_ZeroTimeout_WaitsIndefinitely()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session) { RpcTimeout = TimeSpan.Zero };

        Task<byte[]> pending = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);
        await Task.Delay(150);
        Assert.False(pending.IsCompleted);

        hub.OnReceiveRPCResponseMessage(new ProcedureCallResponseMessage(1u, Payload));
        Assert.Equal(Payload, await pending);
    }

    [Fact]
    public async Task RequestRPC_ErrorResponse_ThrowsRpcFaultException()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        Task<byte[]> pending = hub.RequestRPC(4, Payload, RpcDeliveryMode.ReliableOrdered);
        uint callId = First<ProcedureCallRequestMessage>(session).CallId;

        hub.OnReceiveRPCErrorMessage(new ProcedureCallErrorMessage(callId, RpcErrorCode.Overloaded, "busy"));

        RpcFaultException fault = await Assert.ThrowsAsync<RpcFaultException>(() => pending);
        Assert.Equal(RpcErrorCode.Overloaded, fault.ErrorCode);
        Assert.Equal(callId, fault.CallId);
        Assert.Equal("busy", fault.Message);
    }

    [Fact]
    public async Task UnexpectedResponseOrError_IsIgnoredWithoutFaultingOthers()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        Task<byte[]> pending = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);

        // 대기표 없는 CallId 의 지연·중복 응답은 다른 호출을 잘못된 값으로 완료시키면 안 된다.
        hub.OnReceiveRPCResponseMessage(new ProcedureCallResponseMessage(999u, new byte[] { 7 }));
        hub.OnReceiveRPCErrorMessage(new ProcedureCallErrorMessage(999u, RpcErrorCode.Unhandled, "late"));
        Assert.False(pending.IsCompleted);

        hub.OnReceiveRPCResponseMessage(new ProcedureCallResponseMessage(1u, Payload));
        Assert.Equal(Payload, await pending);
    }

    [Fact]
    public async Task CancelPendingCalls_FailsWaitingRequest()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        Task<byte[]> pending = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);
        hub.CancelPendingCalls(new InvalidOperationException("gone"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
    }

    [Fact]
    public async Task Incoming_UnknownMethod_SendsUnknownMethodError()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(5u, 4242, Payload));

        await WaitUntilAsync(() => session.Sent.Count > 0);
        var error = First<ProcedureCallErrorMessage>(session);
        Assert.Equal(5u, error.CallId);
        Assert.Equal(RpcErrorCode.UnknownMethod, error.ErrorCode);
    }

    [Fact]
    public async Task Incoming_Success_SendsResponse_WithRequestDeliveryMode()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);
        hub.Register(3, data => Task.FromResult(new byte[] { 9 }), RpcDeliveryMode.ReliableUnordered);

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(1u, 3, Payload));

        await WaitUntilAsync(() => session.Sent.Count > 0);
        Assert.Equal(new byte[] { 9 }, First<ProcedureCallResponseMessage>(session).ReturnData);
        Assert.Equal(RudpDeliveryMethod.ReliableUnordered,
            Assert.IsType<RudpSendOptions>(session.SentOptions[0]).DeliveryMethod);
    }

    [Fact]
    public async Task Incoming_ImplementationThrows_SendsUnhandledError()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);
        hub.Register(3, _ => Task.FromException<byte[]>(new InvalidOperationException("boom")));

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(1u, 3, Payload));

        await WaitUntilAsync(() => session.Sent.Count > 0);
        var error = First<ProcedureCallErrorMessage>(session);
        Assert.Equal(RpcErrorCode.Unhandled, error.ErrorCode);
        Assert.Contains("boom", error.Message);
    }

    [Fact]
    public async Task Incoming_OneWay_IsNotAnswered()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);
        var called = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Register(3, _ =>
        {
            called.TrySetResult();
            return Task.FromResult(Payload);
        });

        // one-way 신호는 CallId 0 (수신 측 등록표 아님)
        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(0u, 3, Payload));

        await called.Task;
        Assert.Empty(session.Sent);
    }

    [Fact]
    public async Task Incoming_UnknownMethod_OneWay_IsNotAnswered()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(0u, 4242, Payload));
        await Task.Delay(100);

        Assert.Empty(session.Sent);
    }

    [Fact]
    public async Task MaxConcurrentIncoming_RejectsWhenFull()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session) { MaxConcurrentIncoming = 1 };

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Register(1, _ =>
        {
            started.TrySetResult();
            return gate.Task;
        });

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(1u, 1, Payload));
        await started.Task;

        // 상한 초과 요청은 처리를 기다리지 않고 Overloaded 를 받는다.
        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(2u, 1, Payload));
        await WaitUntilAsync(() => session.Sent.OfType<ProcedureCallErrorMessage>().Any());

        ProcedureCallErrorMessage error = Assert.Single(session.Sent.OfType<ProcedureCallErrorMessage>());
        Assert.Equal(2u, error.CallId);
        Assert.Equal(RpcErrorCode.Overloaded, error.ErrorCode);

        gate.SetResult(Payload);
    }

    [Fact]
    public void MaxConcurrentIncoming_RejectsNegative()
    {
        using var hub = new TestHub(new FakeSession());
        Assert.Throws<ArgumentOutOfRangeException>(() => hub.MaxConcurrentIncoming = -1);
    }

    [Fact]
    public async Task Disconnect_CancelsPending_RaisesOnce_AndDisconnectsSession()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);
        int raised = 0;
        hub.Disconnected += () => raised++;

        Task<byte[]> pending = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);
        hub.Disconnect();
        hub.Disconnect();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Equal(1, raised);
        Assert.Equal(2, session.DisconnectCount);
    }

    [Fact]
    public async Task SessionDisconnectedEvent_CancelsPendingThroughHandler()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);
        var handler = new DRPCMessageHandler(session, hub);
        int raised = 0;
        hub.Disconnected += () => raised++;

        Task<byte[]> pending = hub.RequestRPC(1, Payload, RpcDeliveryMode.ReliableOrdered);
        session.RaiseDisconnected();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Equal(1, raised);
        GC.KeepAlive(handler);
    }

    [Fact]
    public async Task Handler_RoutesEachMessageTypeToHub()
    {
        var session = new FakeSession();
        using var hub = new TestHub(session);
        var handler = new DRPCMessageHandler(session, hub);
        hub.Register(3, data => Task.FromResult(data), RpcDeliveryMode.ReliableOrdered);

        handler.HandleMessage(new ProcedureCallRequestMessage(1u, 3, Payload));
        await WaitUntilAsync(() => session.Sent.Count > 0);
        Assert.IsType<ProcedureCallResponseMessage>(session.Sent[0]);

        // 미등록 타입은 무시만 한다(수신 경로를 죽이지 않는다).
        handler.HandleMessage("not an rpc message");
        Assert.Single(session.Sent);
        GC.KeepAlive(handler);
    }

    [Fact]
    public void RpcDeliveryMap_CoversEveryMode()
    {
        // DRPC 계약 열거형과 RUDP 열거형의 대응이 끊기면 이 테스트가 잡는다.
        Assert.Equal(RudpDeliveryMethod.ReliableOrdered, RpcDeliveryMode.ReliableOrdered.ToSendOptions().DeliveryMethod);
        Assert.Equal(RudpDeliveryMethod.ReliableUnordered, RpcDeliveryMode.ReliableUnordered.ToSendOptions().DeliveryMethod);
        Assert.Equal(RudpDeliveryMethod.Sequenced, RpcDeliveryMode.Sequenced.ToSendOptions().DeliveryMethod);
        Assert.Equal(RudpDeliveryMethod.ReliableSequenced, RpcDeliveryMode.ReliableSequenced.ToSendOptions().DeliveryMethod);
        Assert.Equal(RudpDeliveryMethod.Unreliable, RpcDeliveryMode.Unreliable.ToSendOptions().DeliveryMethod);
        Assert.Equal(Enum.GetValues<RpcDeliveryMode>().Length, Enum.GetValues<RudpDeliveryMethod>().Length);
    }

    static T First<T>(FakeSession session) => (T)session.Sent.OfType<T>().First();

    static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("조건이 예상 시간 내에 충족되지 않았습니다.");
    }
}
