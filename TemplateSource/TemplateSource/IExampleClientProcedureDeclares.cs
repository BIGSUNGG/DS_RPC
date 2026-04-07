using RPC.Attribute;
using RPC.Shared.Interface;

namespace TemplateSource
{
    public interface IExampleClientProcedureDeclarations : IClientProcedureDeclarations
    {
        [RemoteProcedure(ReliableType.Reliable)]
        float Sum(float a, float b);
    }
}
