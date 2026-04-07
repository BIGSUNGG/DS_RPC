using Communication.Network.RUDP.Shared.Messages;
using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using MessageProtocol;
using MessageProtocol.Serialize;
using RPC.Client.Network;
using RPC.Shared;
using RPC.Shared.Interface;
using RPC.Shared.Network;
using System.Reflection;

namespace TemplateSource;

public partial class ExampleClientHub : ServerHub<IExampleServerProcedureDeclartions, IExampleClientProcedureDeclarations>
{
    public ExampleClientHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }

    public new static async Task<ExampleClientHub> ConnectAsync(string host, int port, string connectionKey = "")
    {
        ExampleClientHub? connectedHub = null;
        Connector connector = new(host, port, connectionKey);

        bool isConnected = await connector.ConnectAsync((peer, netManager, listener) =>
        {
            connectedHub = new ExampleClientHub(hub =>
                new ServerSession(
                    peer,
                    netManager,
                    session => new RUDPMessageReceiver(
                        new DefaultMessageConverter(),
                        peer,
                        netManager,
                        listener,
                        new DefaultMessageHandler(session, hub)),
                    _ => new RUDPMessageSender(
                        new DefaultMessageConverter(),
                        peer)));

            return Task.CompletedTask;
        });

        if (isConnected == false || connectedHub == null)
            throw new InvalidOperationException("Failed to connect to server.");

        return connectedHub;
    }

    public float Sum(float a, float b)
    {
        Sum_Parameter parameter = new(a, b);
        byte[] parameterData = MessageSerializer.Serialize(parameter);
        Task<byte[]> RPCTask = RequestRPC(0, parameterData);
        RPCTask.Wait();
        return float.Parse(RPCTask.Result);
    }

    public async Task<float> SumAsync(float a, float b)
    {
        Sum_Parameter parameter = new(a, b);
        byte[] parameterData = MessageSerializer.Serialize(parameter);
        Task<byte[]> RPCTask = RequestRPC(0, parameterData);
        await RPCTask;
        return float.Parse(RPCTask.Result);
    }

    [Message]
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
        private static readonly MethodInfo SerializeMethod = ResolveMethod("Serialize");
        private static readonly MethodInfo DeserializeMethod = ResolveMethod("Deserialize");

        public byte[] Serialize(object message)
        {
            return (byte[])SerializeMethod.Invoke(null, new object[] { message })!;
        }

        public object Deserialize(ReadOnlySpan<byte> messageData)
        {
            return DeserializeMethod.Invoke(null, new object[] { messageData.ToArray() })!;
        }

        private static MethodInfo ResolveMethod(string methodName)
        {
            Type serializerType = Type.GetType("MessageProtocol.Serialize.MessageSerializer, MessageProtocol")
                                  ?? throw new InvalidOperationException("MessageSerializer type was not found.");

            return serializerType
                       .GetMethods(BindingFlags.Public | BindingFlags.Static)
                       .FirstOrDefault(method => method.Name == methodName && method.GetParameters().Length == 1)
                   ?? throw new InvalidOperationException($"{methodName} method was not found on MessageSerializer.");
        }
    }

    private sealed class DefaultMessageHandler : RPCMessageHandler
    {
        public DefaultMessageHandler(ISession session, IHubBase hub)
            : base(session, hub)
        {
        }
    }
}
