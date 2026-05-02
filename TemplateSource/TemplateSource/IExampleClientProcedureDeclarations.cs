using Communication.Network.RUDP.Shared.Messages;
using DRPC.Attribute;
using DRPC.Shared.Interface;

namespace TemplateSource
{
    public interface IExampleClientProcedureDeclarations : IClientProcedureDeclarations
    {
        [RemoteProcedure(ReliableType.ReliableOrdered)]
        float Sum(float a, float b);
    }
}
