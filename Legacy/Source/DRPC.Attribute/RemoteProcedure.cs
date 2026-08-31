using Communication.Network.RUDP.Shared.Messages;

namespace DRPC.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class RemoteProcedure : System.Attribute
{
    public ReliableType ReliableType { get; }
    public int MethodId { get; }

    /// <summary>
    /// true이면 요청만 보내고 응답을 기다리지/보내지 않는다. void 메서드만 허용.
    /// </summary>
    public bool OneWay { get; set; }

    public RemoteProcedure(ReliableType type, int methodId = -1)
    {
        ReliableType = type;
        MethodId = methodId;
    }
}
