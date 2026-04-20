using Examples.Sandbox;
using RPC.Server.Netwrok;
using RPC.Shared.Interface;
using RPC.Shared.Network;

namespace Examples.Sandbox.Server;

public partial class PlaygroundClientHub : ClientHub<IPlaygroundServerProcedureDeclarations, IPlaygroundClientProcedureDeclarations>
{
    private partial int GetBuildId_Implementation()
    {
        Console.WriteLine("GetBuildId() called");
        return 2026;
    }

    private partial int Add_Implementation(int value1, int value2)
    {
        return value1 + value2;
    }

    private partial RegisterResult Register_Implementation(RegisterData message)
    {
        Console.WriteLine($"Register() called with message: Name={message.Name}");
        return new RegisterResult() { Id = 1 };
    }
}
