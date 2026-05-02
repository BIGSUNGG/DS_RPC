using MessageProtocol;
using MessageProtocol.Serialize;
using DRPC.Client.Network;
using DRPC.Shared.Network;

namespace TemplateSource.Client;

public partial class ExampleServerHub : ServerHub<IExampleServerProcedureDeclartions, IExampleClientProcedureDeclarations>
{
    private partial float Sum_Implementation(float a, float b)
    {
        return a + b;
    }
}
