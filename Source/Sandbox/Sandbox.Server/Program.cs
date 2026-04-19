using MessageProtocol;
using MessageProtocol.Serialize;
using Sandbox.Server;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Listening on 0.0.0.0:19050 ...");
await RpcServerHub.ListenAsync(19050, async hub =>
{
    Console.WriteLine("Client connected.");
    var sum = await hub.AddAsync(21f, 21f);
    Console.WriteLine($"Client Add(21,21) => {sum}");

    //MessageSerializer.Serialize<A>(new A());
}, cts.Token);

[GroupRootMessage(1)]
public partial class A
{

}