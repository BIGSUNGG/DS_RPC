using Examples.Sandbox;
using Examples.Sandbox.Client;

using var cts = new CancellationTokenSource();
const string connectionKey = "sandbox-key";
var hub = await PlaygroundServerHub.ConnectAsync("127.0.0.1", 9050, connectionKey, cts.Token);
Console.WriteLine("Connected to server.");
int id = await hub.GetBuildIdAsync();
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