namespace RPC.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class RemoteProcedure : System.Attribute
{
    public  RemoteProcedure(ReliableType type)
    {
    }
}