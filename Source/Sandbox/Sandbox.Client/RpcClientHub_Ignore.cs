using RPC.Client.Network;
using RPC.Shared.Network;
using Sandbox;

namespace Sandbox.Client;

public partial class RpcClientHub : ServerHub<ISandboxServerProcedureDeclarations, ISandboxClientProcedureDeclarations>
{
    private partial float Add_Implementation(float a, float b) => a + b;
}
