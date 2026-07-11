using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using LiteNetLib;

namespace DRPC.Client.Network;

public sealed class ServerSession : RUDPSession
{
    public ServerSession(NetPeer netPeer, NetManager netManager, Func<Session, IMessageReceiver> receiverCreater, Func<Session, IMessageSender> senderCreater)
        : base(netPeer, netManager, receiverCreater, senderCreater)
    {
    }

    protected override void OnDisconnected()
    {
    }
}
