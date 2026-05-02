using Communication.Network.RUDP.Shared.Messages;

namespace DRPC.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class RemoteProcedure : System.Attribute
{
    public  RemoteProcedure(ReliableType type)
    {
    }
}