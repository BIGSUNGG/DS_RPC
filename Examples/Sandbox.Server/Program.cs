using Examples.Sandbox;
using Examples.Sandbox.Server;

using var cts = new CancellationTokenSource();
const string connectionKey = "sandbox-key";
Console.WriteLine("Sandbox server listening on 9050...");

List<PlaygroundClientHub> hubs = new();
_ = PlaygroundClientHub.ListenAsync(
    9050,
    connectionKey,
    async hub =>
    {
        hubs.Add(hub);

        Console.WriteLine("Client connected.");
        float echoed = hub.Echo(3.14f);
        Console.WriteLine($"Echo(3.14f) -> {echoed}");
        float echoedList = hub.Echo_List(new List<float> { 3.14f, 2.71f, 1.41f, 10f });
        Console.WriteLine($"Echo_List() -> {echoedList}");
        await Task.CompletedTask;
    },
    cts.Token);

while(true)
{
    string? input = Console.ReadLine();
    
    PlaygroundMessageGroup message;
    if(input == "0")
    {
        message = null; 
    }
    else if(input == "1")
    {
        message = new PlaygroundMessageGroupElement();
    }
    else
    {
        message = new PlaygroundMessageGroup();
    }

    foreach (var hub in hubs)
    {
        hub.PrintMessage(message);
    }

}