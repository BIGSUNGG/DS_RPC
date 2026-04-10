using Communication.Network.RUDP.Shared.Messages;
using RPC.Attribute;
using RPC.Shared.Interface;

namespace TemplateSource
{
    public interface IExampleClientProcedureDeclarations : IClientProcedureDeclarations
    {
        [RemoteProcedure(ReliableType.ReliableOrdered)]
        float Sum(float a, float b);
    }
}
