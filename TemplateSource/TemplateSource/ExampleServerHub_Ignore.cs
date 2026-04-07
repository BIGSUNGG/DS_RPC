using Communication.Shared.Sessions;
using MessageProtocol;
using MessageProtocol.Serialize;
using RPC.Client.Network;
using RPC.Shared.Network;

namespace TemplateSource;

public partial class ExampleServerHub : ServerHub<IExampleServerProcedureDeclartions,IExampleClientProcedureDeclarations>
{
    private partial float Sum_Implementation(float a, float b)
    {
        return a + b;
    }
}