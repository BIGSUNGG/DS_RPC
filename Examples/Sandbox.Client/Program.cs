using Examples.Sandbox;
using Examples.Sandbox.Client;

using var cts = new CancellationTokenSource();
var hub = await PlaygroundServerHub.ConnectAsync("127.0.0.1", 9050, cts.Token);
int id = hub.GetBuildId();
Console.WriteLine($"GetBuildId() -> {id}");
