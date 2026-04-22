using Examples.Sandbox;
using Examples.Sandbox.Server;

using var cts = new CancellationTokenSource();
const string connectionKey = "sandbox-key";
Console.WriteLine("Sandbox server listening on 9050...");
await PlaygroundClientHub.ListenAsync(
    9050,
    connectionKey,
    async hub =>
    {
        Console.WriteLine("Client connected.");
        float echoed = hub.Echo(3.14f);
        Console.WriteLine($"Echo(3.14f) -> {echoed}");
        float echoedList = hub.Echo_List(new List<float> { 3.14f, 2.71f, 1.41f, 10f });
        Console.WriteLine($"Echo_List() -> {echoedList}");
        hub.PrintMessage(new PlaygroundMessageGroupElement(), 42);
        await Task.CompletedTask;
    },
    cts.Token);
