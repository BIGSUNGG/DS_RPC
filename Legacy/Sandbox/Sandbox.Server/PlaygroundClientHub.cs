using Sandbox;
using DRPC.Server.Netwrok;
using DRPC.Shared.Interface;
using DRPC.Shared.Network;

namespace Sandbox.Server;

public partial class PlaygroundClientHub : ClientHub<IPlaygroundServerProcedureDeclarations, IPlaygroundClientProcedureDeclarations>
{
    private partial Task<int> GetBuildId_Implementation()
    {
        Console.WriteLine("GetBuildId() called");
        return Task.FromResult(2026);
    }

    private partial Task<int> Add_Implementation(int value1, int value2)
    {
        return Task.FromResult(value1 + value2);
    }

    private partial Task<RegisterResult> Register_Implementation(int id, RegisterData message)
    {
        Console.WriteLine($"Register() called with message: Name={message.Name}");
        return Task.FromResult(new RegisterResult() { Id = id });
    }
}
