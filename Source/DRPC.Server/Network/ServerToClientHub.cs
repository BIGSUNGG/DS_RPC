using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace DRPC.Server.Netwrok;

/// <summary>
/// 서버가 클라이언트 peer마다 만드는 Hub. <see cref="ClientHub{T1,T2}"/>의 명확한 별칭.
/// </summary>
public abstract class ServerToClientHub<T1, T2> : ClientHub<T1, T2>
    where T1 : IServerProcedureDeclarations
    where T2 : IClientProcedureDeclarations
{
    protected ServerToClientHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}
