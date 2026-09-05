using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace DRPC.Server.Network;

/// <summary>
/// 서버가 peer 마다 상속하는 허브 베이스(ADR-0001: 소유 주체 = 서버).
/// 생성된 partial 파생 타입은 <c>ListenAsync</c> 와 클라이언트 계약(<typeparamref name="TCPD"/>)의 outgoing 스텁,
/// 서버 계약(<typeparamref name="TSPD"/>)의 incoming 디스패치를 얻는다.
/// </summary>
/// <typeparam name="TSPD">서버가 구현하는 함수 선언 인터페이스.</typeparam>
/// <typeparam name="TCPD">클라이언트가 구현하는 함수 선언 인터페이스.</typeparam>
public abstract class ServerHub<TSPD, TCPD> : HubBase<TSPD, TCPD>
    where TSPD : IServerProcedureDeclarations
    where TCPD : IClientProcedureDeclarations
{
    protected ServerHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}
