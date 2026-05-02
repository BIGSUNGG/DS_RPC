using MessageProtocol;
using MessageProtocol.Serialize;
using DRPC.Client.Network;
using DRPC.Server.Netwrok;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace TemplateSource.Server;

public partial class ExampleClientHub : ClientHub<IExampleServerProcedureDeclartions, IExampleClientProcedureDeclarations>
{
    private partial int GetAnswer_Implementation()
    {
        return 42;
    }
}
