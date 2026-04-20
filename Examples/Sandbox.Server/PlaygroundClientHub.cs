using Examples.Sandbox;
using RPC.Server.Netwrok;
using RPC.Shared.Interface;
using RPC.Shared.Network;

namespace Examples.Sandbox.Server;

public partial class PlaygroundClientHub : ClientHub<IPlaygroundServerProcedureDeclarations, IPlaygroundClientProcedureDeclarations>
{
    private partial int GetBuildId_Implementation()
    {
        return 2026;
    }
}
