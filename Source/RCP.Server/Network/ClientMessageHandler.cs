using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using System;

namespace RPC.Server.Netwrok;

public sealed class ClientMessageHandler : MessageHandler
{
    public ClientMessageHandler(ISession session)
        : base(session)
    {
    }

    protected override void RegisterMessageType()
    {
        //_messageHandleActions.Add(typeof(C_ChatSendMessage), HandleChatSendMessage);
    }
}
