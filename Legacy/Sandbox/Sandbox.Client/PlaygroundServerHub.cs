using Sandbox;
using DRPC.Client.Network;
using DRPC.Shared.Network;

namespace Sandbox.Client;

public partial class PlaygroundServerHub : ServerHub<IPlaygroundServerProcedureDeclarations, IPlaygroundClientProcedureDeclarations>
{
    private partial Task<float> Echo_Implementation(float value)
    {
        return Task.FromResult(value * 2f);
    }

    private partial Task<float> Echo_List_Implementation(List<float> value)
    {
        return Task.FromResult(value.Sum());
    }

    private partial Task PrintMessage_Implementation(PlaygroundMessageGroup message)
    {
        if (message != null)
            message.PrintMessage();
        else
            Console.WriteLine("Received null message");
        return Task.CompletedTask;
    }
}
