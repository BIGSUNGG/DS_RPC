using Communication.Network.RUDP.Shared.Messages;
using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using MessageProtocol;
using MessageProtocol.Serialize;
using RPC.Client.Network;
using RPC.Server.Netwrok;
using RPC.Shared;
using RPC.Shared.Interface;
using RPC.Shared.Network;
using System.Reflection;

namespace TemplateSource;

public partial class ExampleServerHub
{
    public ExampleServerHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
        MethodCallActions.Add(0, Sum_Requested);
    }

    public static async Task<ExampleServerHub> ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        return await ConnectAsync(host, port, "", cancellationToken);
    }

    public static async Task<ExampleServerHub> ConnectAsync(string host, int port, string connectionKey, CancellationToken cancellationToken)
    {
        ExampleServerHub? connectedHub = null;
        Connector connector = new(host, port, connectionKey);

        bool isConnected = await connector.ConnectAsync((peer, netManager, listener) =>
        {
            connectedHub = new ExampleServerHub(hub =>
                new ServerSession(
                    peer,
                    netManager,
                    session => new RUDPMessageReceiver(
                        new DefaultMessageConverter(),
                        peer,
                        netManager,
                        listener,
                        new RPCMessageHandler(session, hub)),
                    _ => new RUDPMessageSender(
                        new DefaultMessageConverter(),
                        peer)));
            
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    netManager.PollEvents();
                    await Task.Delay(10);
                }
            }, cancellationToken);
            
            return Task.CompletedTask;
        });

        if (isConnected == false || connectedHub == null)
            throw new InvalidOperationException("Failed to connect to server.");

        return connectedHub;
    }


    private byte[] Sum_Requested(byte[] parameterData)
    {
        Sum_Parameter parameter = MessageSerializer.Deserialize<Sum_Parameter>(parameterData);
        float result = Sum_Implementation(parameter.a, parameter.b);
        return BitConverter.GetBytes(result);
    }
    private partial float Sum_Implementation(float a, float b);

    [NonIdMessage]
    public partial class Sum_Parameter
    {
        public float a;
        public float b;

        public Sum_Parameter()
        {
        }

        public Sum_Parameter(float a, float b)
        {
            this.a = a;
            this.b = b;
        }
    }

    private sealed class DefaultMessageConverter : IMessageConverter
    {

        public byte[] Serialize(object message)
        {
            return MessageSerializer.Serialize(message);
        }

        public object Deserialize(ReadOnlySpan<byte> messageData)
        {
            return MessageSerializer.Deserialize(messageData.ToArray());
        }
    }
}


[NonIdMessage]
public partial class ExampleMessage
{
    public int Value;

    public ExampleMessage()
    {
    }

    public ExampleMessage(int value)
    {
        Value = value;
    }
}