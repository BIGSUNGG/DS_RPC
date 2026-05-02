using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using LiteNetLib;
using System;

namespace DRPC.Client.Network;

public sealed class ServerSession : RUDPSession
{
    public ServerSession(NetPeer netPeer, NetManager netManager, Func<Session, IMessageReceiver> receiverCreater, Func<Session, IMessageSender> senderCreater)
        : base(netPeer, netManager, receiverCreater, senderCreater)
    {
    }

    protected override void OnDisconnected()
    {
        Console.WriteLine("서버와의 연결이 종료되었습니다.");
    }
}
