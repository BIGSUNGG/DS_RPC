using Communication.Shared.Channels;
using Communication.Shared.Connection;
using Communication.Shared.Sessions;
using DRPC.Shared.Message;
using DRPC.Shared.Network;

namespace DRPC.Shared.Tests;

/// <summary>
/// 전송 스택 없이 Hub 런타임만 검증하기 위한 메모리 세션. 보낸 메시지를 순서대로 기록한다.
/// </summary>
internal sealed class FakeSession : ISession
{
    public List<object> Sent { get; } = new();
    public List<SendOptions?> SentOptions { get; } = new();
    public int DisconnectCount { get; private set; }
    public Func<object, bool>? OnSend { get; set; }

    public Task SendAsync(object message) => SendAsync(message, null);

    public Task SendAsync(object message, SendOptions? options)
    {
        Sent.Add(message);
        SentOptions.Add(options);
        OnSend?.Invoke(message);
        return Task.CompletedTask;
    }

    public Task SendAndFlushAsync(object message, SendOptions? options = null, CancellationToken cancellationToken = default)
        => SendAsync(message, options);

    public void Disconnect() => DisconnectCount++;

    public bool IsConnected() => DisconnectCount == 0;

    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    public void RaiseDisconnected(DisconnectReason reason = DisconnectReason.Remote)
        => Disconnected?.Invoke(this, new DisconnectedEventArgs(reason));

    public T Expect<T>(int index) => (T)Sent[index];

    public void Clear()
    {
        Sent.Clear();
        SentOptions.Clear();
    }

    public void Dispose()
    {
    }
}

/// <summary>HubBase 의 protected 표면(SendRPC/RequestRPC, 등록 딕셔너리)을 테스트에 노출한다.</summary>
internal sealed class TestHub : HubBase
{
    public TestHub(ISession session)
        : base(_ => session)
    {
    }

    public void Register(int methodId, Func<byte[], Task<byte[]>> action,
        RpcDeliveryMode mode = RpcDeliveryMode.ReliableOrdered)
    {
        MethodCallActions[methodId] = action;
        MethodDeliveryModes[methodId] = mode;
    }

    public new Task SendRPC(int methodId, byte[] parameterData, RpcDeliveryMode mode)
        => base.SendRPC(methodId, parameterData, mode);

    public new Task<byte[]> RequestRPC(int methodId, byte[] parameterData, RpcDeliveryMode mode)
        => base.RequestRPC(methodId, parameterData, mode);
}
