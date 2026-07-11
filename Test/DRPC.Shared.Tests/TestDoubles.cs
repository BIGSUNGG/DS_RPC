using Communication.Network.RUDP.Shared.Messages;
using Communication.Shared.Sessions;
using DRPC.Shared.Message;
using DRPC.Shared.Network;

namespace DRPC.Shared.Tests;

internal sealed class FakeSession : ISession
{
    public List<object> Sent { get; } = new();
    public int DisconnectCount { get; private set; }

    public Task SendAsync(object message)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }

    public Task SendAsync(object message, object context)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }

    public void Disconnect() => DisconnectCount++;
}

/// <summary>HubBase protected API를 테스트용으로 노출.</summary>
internal sealed class TestHub : HubBase
{
    public TestHub(ISession session)
        : base(_ => session)
    {
    }

    public void Register(int methodId, Func<byte[], Task<byte[]>> action, ReliableType reliable = ReliableType.ReliableOrdered, bool oneWay = false)
    {
        MethodCallActions[methodId] = action;
        MethodReliableTypes[methodId] = reliable;
        if (oneWay)
        {
            OneWayMethodIds.Add(methodId);
        }
    }

    public Task SendOneWayAsync(int methodId, byte[] data, ReliableType reliable = ReliableType.ReliableOrdered)
        => SendRPC(methodId, data, reliable);

    public Task<byte[]> RequestAsync(int methodId, byte[] data, ReliableType reliable = ReliableType.ReliableOrdered)
        => RequestRPC(methodId, data, reliable);
}
