using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using LiteNetLib;
using System;

namespace RPC.Server.Netwrok;

public sealed class ClientSession : RUDPSession
{
    public int SessionId { get; set; }

    public ClientSession(int sessionId, NetPeer netPeer, NetManager netManager, Func<Session, IMessageReceiver> receiverCreater, Func<Session, IMessageSender> senderCreater)
        : base(netPeer, netManager, receiverCreater, senderCreater)
    {
        SessionId = sessionId;
    }

    protected override void OnDisconnected()
    {
        Console.WriteLine($"클라이언트 {SessionId} 연결 종료");
    }
}
