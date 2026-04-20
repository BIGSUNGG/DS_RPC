using MessageProtocol;
using MessageProtocol.Serialize;
using RPC.Client.Network;
using RPC.Server.Netwrok;
using RPC.Shared.Interface;
using RPC.Shared.Network;

namespace TemplateSource.Server;

public partial class ExampleClientHub : ClientHub<IExampleServerProcedureDeclartions, IExampleClientProcedureDeclarations>
{
    private partial int GetAnswer_Implementation()
    {
        return 42;
    }
}
