using Communication.Network.RUDP.Shared.Messages;
using RPC.Attribute;
using RPC.Shared.Interface;

namespace TemplateSource
{
    public interface IExampleServerProcedureDeclartions : IServerProcedureDeclarations
    {
        [RemoteProcedure(ReliableType.ReliableOrdered)]
        int GetAnswer();
    }
}
