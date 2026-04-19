using Sandbox.Client;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Connecting to 127.0.0.1:19050 ...");
var hub = await RpcClientHub.ConnectAsync("127.0.0.1", 19050, cts.Token);
var ver = await hub.GetProtocolVersionAsync();
Console.WriteLine($"Server protocol version: {ver}");
