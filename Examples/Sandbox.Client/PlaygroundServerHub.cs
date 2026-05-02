using Examples.Sandbox;
using DRPC.Client.Network;
using DRPC.Shared.Network;

namespace Examples.Sandbox.Client;

public partial class PlaygroundServerHub : ServerHub<IPlaygroundServerProcedureDeclarations, IPlaygroundClientProcedureDeclarations>
{
    private partial float Echo_Implementation(float value)
    {
        return value * 2f;
    }

    private partial float Echo_List_Implementation(List<float> value)
    {
        return value.Sum();
    }

    private partial void PrintMessage_Implementation(PlaygroundMessageGroup message)
    {
        if(message != null)
            message.PrintMessage();
        else
            Console.WriteLine("Received null message");
    }
}
