using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using System;
using DRPC.Shared.Interface;
using DRPC.Shared.Message;

namespace DRPC.Shared;

public class DRPCMessageHandler : MessageHandler
{
    IHubBase _hub;
    
    public DRPCMessageHandler(ISession session, IHubBase hub)
        : base(session)
    {
        _hub = hub;
    }

    protected override void RegisterMessageType()
    {
        _messageHandleActions.Add(typeof(ProcedureCallRequestMessage), HandleProcedureCallRequestMessage);
        _messageHandleActions.Add(typeof(ProcedureCallResponseMessage), HandleProcedureCallResponseMessage);
    }

    private void HandleProcedureCallRequestMessage(object obj)
    {
        ProcedureCallRequestMessage  requestMessage = (ProcedureCallRequestMessage)obj;
        _hub.OnReceiveRPCRequestMessage(requestMessage);
    }
    
    private void HandleProcedureCallResponseMessage(object obj)
    {
        ProcedureCallResponseMessage  responseMessage = (ProcedureCallResponseMessage)obj;
        _hub.OnReceiveRPCResponseMessage(responseMessage);
    }
}
