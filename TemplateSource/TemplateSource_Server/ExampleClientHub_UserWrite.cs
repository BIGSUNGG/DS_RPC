using MessageProtocol;
using MessageProtocol.Serialize;
using DRPC.Client.Network;
using DRPC.Server.Netwrok;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace TemplateSource.Server;

public partial class ExampleClientHub : ClientHub<IExampleServerProcedureDeclarations, IExampleClientProcedureDeclarations>
{
    private partial Task<int> GetAnswer_Implementation()
    {
        return Task.FromResult(42);
    }
}
