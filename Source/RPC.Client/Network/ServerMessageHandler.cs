using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using System;

namespace RPC.Client.Network;

public sealed class ServerMessageHandler : MessageHandler
{
    public ServerMessageHandler(ISession session)
        : base(session)
    {
    }

    protected override void RegisterMessageType()
    {
        //_messageHandleActions.Add(typeof(S_ChatNotifyMessage), HandleChatNotifyMessage);
    }

    private void HandleChatNotifyMessage(object message)
    {
        // S_ChatNotifyMessage chatMessage = (S_ChatNotifyMessage)message;
        // Console.WriteLine($"{chatMessage.SenderName}: {chatMessage.Content}");
    }
}
