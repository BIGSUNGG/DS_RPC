using Communication.Network.RUDP.Shared.Messages;
using DRPC.Attribute;
using DRPC.Shared.Interface;

namespace TemplateSource
{
    public interface IExampleServerProcedureDeclarations : IServerProcedureDeclarations
    {
        [RemoteProcedure(ReliableType.ReliableOrdered, 0)]
        int GetAnswer();
    }
}
