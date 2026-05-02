using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using LiteNetLib;
using System;

namespace DRPC.Server.Netwrok;

public sealed class ClientSession : RUDPSession
{
    public ClientSession(NetPeer netPeer, NetManager netManager, Func<Session, IMessageReceiver> receiverCreater, Func<Session, IMessageSender> senderCreater)
        : base(netPeer, netManager, receiverCreater, senderCreater)
    {
    }

    protected override void OnDisconnected()
    {
        Console.WriteLine("클라이언트 연결 종료");
    }
}
