using RPC.Server.Netwrok;
using RPC.Shared.Network;
using Sandbox;

namespace Sandbox.Server;

public partial class RpcServerHub : ClientHub<ISandboxServerProcedureDeclarations, ISandboxClientProcedureDeclarations>
{
    private partial int GetProtocolVersion_Implementation() => 42;
}
