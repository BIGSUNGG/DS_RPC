using Communication.Network.RUDP.Shared.Messages;
using DRPC.Shared.Message;
using Xunit;

namespace DRPC.Shared.Tests;

public class HubBaseTests
{
    [Fact]
    public async Task SendRPC_OneWay_UsesCallIdZero()
    {
        var session = new FakeSession();
        var hub = new TestHub(session);

        await hub.SendOneWayAsync(7, new byte[] { 1, 2, 3 });

        var req = Assert.IsType<ProcedureCallRequestMessage>(Assert.Single(session.Sent));
        Assert.Equal(0u, req.CallId);
        Assert.Equal(7, req.MethodId);
    }

    [Fact]
    public async Task RequestRPC_Timeout_ThrowsTimeoutException()
    {
        var session = new FakeSession();
        var hub = new TestHub(session) { RpcTimeout = TimeSpan.FromMilliseconds(200) };

        await Assert.ThrowsAsync<TimeoutException>(() =>
            hub.RequestAsync(1, Array.Empty<byte>()));
    }

    [Fact]
    public async Task RequestRPC_ErrorResponse_ThrowsRpcFaultException()
    {
        var session = new FakeSession();
        var hub = new TestHub(session) { RpcTimeout = Timeout.InfiniteTimeSpan };

        var requestTask = hub.RequestAsync(1, Array.Empty<byte>());
        await Task.Delay(20);

        var sent = Assert.IsType<ProcedureCallRequestMessage>(Assert.Single(session.Sent));
        hub.OnReceiveRPCErrorMessage(new ProcedureCallErrorMessage(sent.CallId, RpcErrorCode.Unhandled, "boom"));

        var ex = await Assert.ThrowsAsync<RpcFaultException>(() => requestTask);
        Assert.Equal(RpcErrorCode.Unhandled, ex.ErrorCode);
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task CancelPendingCalls_FailsWaitingRequest()
    {
        var session = new FakeSession();
        var hub = new TestHub(session) { RpcTimeout = Timeout.InfiniteTimeSpan };

        var requestTask = hub.RequestAsync(1, Array.Empty<byte>());
        await Task.Delay(20);
        hub.CancelPendingCalls(new InvalidOperationException("gone"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => requestTask);
    }

    [Fact]
    public async Task MaxConcurrentIncoming_RejectsWhenFull()
    {
        var session = new FakeSession();
        var hub = new TestHub(session) { MaxConcurrentIncoming = 1 };
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.Register(1, async _ =>
        {
            entered.TrySetResult();
            await release.Task;
            return Array.Empty<byte>();
        });

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(10, 1, Array.Empty<byte>()));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(11, 1, Array.Empty<byte>()));
        await Task.Delay(50);

        var error = session.Sent.OfType<ProcedureCallErrorMessage>().Single();
        Assert.Equal(11u, error.CallId);
        Assert.Equal(RpcErrorCode.Overloaded, error.ErrorCode);

        release.TrySetResult();
        await Task.Delay(50);
    }

    [Fact]
    public async Task Incoming_Success_SendsResponse()
    {
        var session = new FakeSession();
        var hub = new TestHub(session);
        hub.Register(3, _ => Task.FromResult(new byte[] { 9 }));

        hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(42, 3, Array.Empty<byte>()));
        await Task.Delay(50);

        var response = Assert.IsType<ProcedureCallResponseMessage>(Assert.Single(session.Sent));
        Assert.Equal(42u, response.CallId);
        Assert.Equal(new byte[] { 9 }, response.ReturnData);
    }

    [Fact]
    public void Disconnect_CallsSessionDisconnect_AndRaisesEvent()
    {
        var session = new FakeSession();
        var hub = new TestHub(session);
        var raised = false;
        hub.Disconnected += () => raised = true;

        hub.Disconnect();

        Assert.Equal(1, session.DisconnectCount);
        Assert.True(raised);
    }
}
