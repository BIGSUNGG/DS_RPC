using Communication.Network.RUDP.Shared.Messages;
using DRPC.Attribute;
using DRPC.Shared.Interface;

namespace TemplateSource
{
    public interface IExampleServerProcedureDeclartions : IServerProcedureDeclarations
    {
        [RemoteProcedure(ReliableType.ReliableOrdered)]
        int GetAnswer();
    }
}
