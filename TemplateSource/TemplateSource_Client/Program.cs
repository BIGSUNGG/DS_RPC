using TemplateSource.Client;

Console.ReadLine();
var hub = await ExampleServerHub.ConnectAsync("localhost", 12345, CancellationToken.None);

Console.WriteLine("Connected to server.");

while (true)
{
        await Task.Delay(1000);
}

