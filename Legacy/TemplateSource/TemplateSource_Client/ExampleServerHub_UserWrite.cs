using MessageProtocol;
using MessageProtocol.Serialize;
using DRPC.Client.Network;
using DRPC.Shared.Network;

namespace TemplateSource.Client;

public partial class ExampleServerHub : ServerHub<IExampleServerProcedureDeclarations, IExampleClientProcedureDeclarations>
{
    private partial Task<float> Sum_Implementation(float a, float b)
    {
        return Task.FromResult(a + b);
    }
}
