using Communication.Network.RUDP.Shared.Messages;
using MessageProtocol;
using RPC.Attribute;
using RPC.Shared.Interface;
using System.Diagnostics.Tracing;

namespace Examples.Sandbox;

public interface IPlaygroundServerProcedureDeclarations : IServerProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    int GetBuildId();
    
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    int Add(int value1, int value2);
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    RegisterResult Register(int id, RegisterData message);
}

public interface IPlaygroundClientProcedureDeclarations : IClientProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    float Echo(float value);

    [RemoteProcedure(ReliableType.ReliableOrdered)]
    float Echo_List(List<float> value);


    [RemoteProcedure(ReliableType.ReliableOrdered)]
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

