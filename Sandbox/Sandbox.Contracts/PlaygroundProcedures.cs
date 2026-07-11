using Communication.Network.RUDP.Shared.Messages;
using MessageProtocol;
using DRPC.Attribute;
using DRPC.Shared.Interface;

namespace Sandbox;

public interface IPlaygroundServerProcedureDeclarations : IServerProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered, 0)]
    int GetBuildId();

    [RemoteProcedure(ReliableType.ReliableOrdered, 1)]
    int Add(int value1, int value2);

    [RemoteProcedure(ReliableType.ReliableOrdered, 2)]
    RegisterResult Register(int id, RegisterData message);
}

public interface IPlaygroundClientProcedureDeclarations : IClientProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered, 0)]
    float Echo(float value);

    [RemoteProcedure(ReliableType.ReliableOrdered, 1)]
    float Echo_List(List<float> value);

    [RemoteProcedure(ReliableType.ReliableOrdered, 2, OneWay = true)]
    void PrintMessage(PlaygroundMessageGroup message);
}

[NonIdMessage]
public partial class RegisterData
{
    public string Name { get; set; } = string.Empty;
}

[NonIdMessage]
public partial class RegisterResult
{
    public int Id { get; set; }
}

[GroupRootMessage(11)]
public partial class PlaygroundMessageGroup
{
    virtual public void PrintMessage()
    {
        Console.WriteLine("PlaygroundMessageGroup");
    }
}

[GroupElementMessage(0)]
public partial class PlaygroundMessageGroupElement : PlaygroundMessageGroup
{
    public override void PrintMessage()
    {
        Console.WriteLine("PlaygroundMessageGroupElement");
    }
}
