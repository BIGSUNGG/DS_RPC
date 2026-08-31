using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace DRPC.Client.Network;

/// <summary>
/// 클라이언트가 서버에 연결할 때 쓰는 Hub. <see cref="ServerHub{T1,T2}"/>의 명확한 별칭.
/// </summary>
public abstract class ClientToServerHub<T1, T2> : ServerHub<T1, T2>
    where T1 : IServerProcedureDeclarations
    where T2 : IClientProcedureDeclarations
{
    protected ClientToServerHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}
