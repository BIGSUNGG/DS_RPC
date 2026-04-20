using Examples.Sandbox;
using RPC.Client.Network;
using RPC.Shared.Network;

namespace Examples.Sandbox.Client;

public partial class PlaygroundServerHub : ServerHub<IPlaygroundServerProcedureDeclarations, IPlaygroundClientProcedureDeclarations>
{
    private partial float Echo_Implementation(float value)
    {
        return value * 2f;
    }
}
