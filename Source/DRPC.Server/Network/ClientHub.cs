using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace DRPC.Server.Netwrok;

/// <summary>
/// RPC 통신을 위한 허브
/// </summary>
/// <typeparam name="T1">서버에서 구현한 함수 선언을 가지는 객체</typeparam>
/// <typeparam name="T2">클라이언트에서 구현한 함수 선언을 가지는 객체</typeparam>
public abstract class ClientHub<T1, T2> : HubBase<T1, T2>
    where T1 : IServerProcedureDeclarations
    where T2 : IClientProcedureDeclarations
{
    public ClientHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}
