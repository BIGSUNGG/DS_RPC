using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Message;

namespace DRPC.Shared;

/// <summary>
/// 수신 메시지를 Hub 런타임으로 라우팅하는 <see cref="MessageHandler"/>.
/// 전송 콜백은 이 핸들러를 동기 호출로 부르고 Hub 는 즉시 직렬화·큐잉만 수행한다.
/// </summary>
public sealed class DRPCMessageHandler : MessageHandler
{
    readonly IHubBase _hub;

    public DRPCMessageHandler(ISession session, IHubBase hub)
        : base(session)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

        Register<ProcedureCallRequestMessage>(message => _hub.OnReceiveRPCRequestMessage(message));
        Register<ProcedureCallResponseMessage>(message => _hub.OnReceiveRPCResponseMessage(message));
        Register<ProcedureCallErrorMessage>(message => _hub.OnReceiveRPCErrorMessage(message));

        // 끊김은 세션 이벤트가 유일한 신호다(하트비트는 전송 계층 영역).
        Session.Disconnected += OnSessionDisconnected;
    }

    void OnSessionDisconnected(object? sender, Communication.Shared.Connection.DisconnectedEventArgs e)
        => _hub.NotifyDisconnected(new InvalidOperationException($"RPC session disconnected ({e.Reason})."));
}
