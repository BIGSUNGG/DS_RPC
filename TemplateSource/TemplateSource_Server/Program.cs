using TemplateSource.Server;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var a = new A();
        await ExampleClientHub.ListenAsync(12345, a.OnConnected, CancellationToken.None);

        while(true)
        {             
            await Task.Delay(1000);
        }
    }

}

public class A
{
    public async Task OnConnected(ExampleClientHub hub)
    {
        Console.WriteLine("Client connected.");
        float result = await hub.SumAsync(1, 2);
        Console.WriteLine($"Sum result: {result}");
    }
}