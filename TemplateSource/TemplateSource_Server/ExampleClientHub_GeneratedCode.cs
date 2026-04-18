using Communication.Network.RUDP.Shared.Messages;
using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using LiteNetLib;
using MessageProtocol;
using MessageProtocol.Serialize;
using RPC.Client.Network;
using RPC.Server.Netwrok;
using RPC.Shared;
using RPC.Shared.Interface;
using RPC.Shared.Network;
using System.Reflection;
using System.Security.Cryptography;

namespace TemplateSource.Server;

public partial class ExampleClientHub
{
    public ExampleClientHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }

    public static Task ListenAsync(int port, Func<ExampleClientHub, Task> onConnected, CancellationToken cancellationToken)
    {
        return ListenAsync(port, "", onConnected, cancellationToken);
    }

    public static async Task ListenAsync(int port, string connectionKey, Func<ExampleClientHub, Task> onConnected, CancellationToken cancellationToken)
    {
        Listener listener = new(System.Net.IPAddress.Any, port, connectionKey);
        int sessionId = 0;

        await listener.ListenAsync(async (peer, netManager, eventListener) =>
        {
            ExampleClientHub hub = new(_ =>
                new ClientSession(
                    Interlocked.Increment(ref sessionId),
                    peer,
                    netManager,
                    session => new RUDPMessageReceiver(
                        new DefaultMessageConverter(),
                        peer,
                        netManager,
                        eventListener,
                        new RPCMessageHandler(session, _)),
                    __ => new RUDPMessageSender(
                        new DefaultMessageConverter(),
                        peer)));

            await onConnected(hub);

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    netManager.PollEvents();
                    await Task.Delay(10);
                }
            }, cancellationToken);
        }, cancellationToken);
    }

    public float Sum(float a, float b)
    {
        Sum_Parameter parameter = new(a, b);
        byte[] parameterData = MessageSerializer.Serialize(parameter);
        Task<byte[]> RPCTask = RequestRPC(0, parameterData, ReliableType.ReliableOrdered);
        RPCTask.Wait();
        return BitConverter.ToSingle(RPCTask.Result, 0);
    }

    public async Task<float> SumAsync(float a, float b)
    {
        Sum_Parameter parameter = new(a, b);
        byte[] parameterData = MessageSerializer.Serialize(parameter);
        Task<byte[]> RPCTask = RequestRPC(0, parameterData, ReliableType.ReliableOrdered);
        await RPCTask;
        return BitConverter.ToSingle(RPCTask.Result, 0);
    }

    [NonIdMessage]
    partial struct Sum_Parameter
    {
        public float a;
        public float b;

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
