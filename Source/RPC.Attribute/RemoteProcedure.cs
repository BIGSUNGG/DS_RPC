using Communication.Network.RUDP.Shared.Messages;

namespace RPC.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class RemoteProcedure : System.Attribute
{
    public  RemoteProcedure(ReliableType type)
    {
    }
}