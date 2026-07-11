using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Message;
using DRPC.Shared.Network;

namespace DRPC.Shared;

public class DRPCMessageHandler : MessageHandler
{
    readonly IHubBase _hub;

    public DRPCMessageHandler(ISession session, IHubBase hub)
        : base(session)
    {
        _hub = hub;
    }

    protected override void RegisterMessageType()
    {
        _messageHandleActions.Add(typeof(ProcedureCallRequestMessage), HandleProcedureCallRequestMessage);
        _messageHandleActions.Add(typeof(ProcedureCallResponseMessage), HandleProcedureCallResponseMessage);
        _messageHandleActions.Add(typeof(ProcedureCallErrorMessage), HandleProcedureCallErrorMessage);
    }

    private void HandleProcedureCallRequestMessage(object obj)
    {
        ProcedureCallRequestMessage requestMessage = (ProcedureCallRequestMessage)obj;
        _hub.OnReceiveRPCRequestMessage(requestMessage);
    }

    private void HandleProcedureCallResponseMessage(object obj)
    {
        ProcedureCallResponseMessage responseMessage = (ProcedureCallResponseMessage)obj;
        _hub.OnReceiveRPCResponseMessage(responseMessage);
    }

    private void HandleProcedureCallErrorMessage(object obj)
    {
        ProcedureCallErrorMessage errorMessage = (ProcedureCallErrorMessage)obj;
        _hub.OnReceiveRPCErrorMessage(errorMessage);
    }

    public override void OnDetectedDisconnection()
    {
        if (_hub is HubBase hubBase)
        {
            hubBase.NotifyDisconnected(new InvalidOperationException("RPC session disconnected."));
        }
        else
        {
            _hub.CancelPendingCalls(new InvalidOperationException("RPC session disconnected."));
        }

        base.OnDetectedDisconnection();
    }
}
