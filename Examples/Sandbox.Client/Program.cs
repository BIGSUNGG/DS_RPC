using Examples.Sandbox;
using Examples.Sandbox.Client;

using var cts = new CancellationTokenSource();
var hub = await PlaygroundServerHub.ConnectAsync("127.0.0.1", 9050, cts.Token);
int id = hub.GetBuildId();
Console.WriteLine("Connected to server.");
Console.WriteLine($"GetBuildId() -> {id}");
var userId = await hub.RegisterAsync(5, new RegisterData { Name = "Test" });
Console.WriteLine($"Register() -> Id={userId.Id}");
while (true)
{
    int value1 = int.Parse(Console.ReadLine() ?? "0");
    int value2 = int.Parse(Console.ReadLine() ?? "0");
    var result = await hub.AddAsync(value1, value2);
    Console.WriteLine($"Add({value1}, {value2}) -> {result}");
}