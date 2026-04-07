using Communication.Network.RUDP.Shared.Messages;
using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using MessageProtocol;
using MessageProtocol.Serialize;
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

    public static Task ListenAsync(int port, Func<ExampleServerHub, Task> onConnected)
    {
        return ListenAsync(port, "", onConnected);
    }

    public static async Task ListenAsync(int port, string connectionKey, Func<ExampleServerHub, Task> onConnected)
    {
        Listener listener = new(System.Net.IPAddress.Any, port, connectionKey);
        int sessionId = 0;

        await listener.ListenAsync(async (peer, netManager, eventListener) =>
        {
            ExampleServerHub hub = new(_ =>
                new ClientSession(
                    Interlocked.Increment(ref sessionId),
                    peer,
                    netManager,
                    session => new RUDPMessageReceiver(
                        new DefaultMessageConverter(),
                        peer,
                        netManager,
                        eventListener,
                        new DefaultMessageHandler(session, _)),
                    __ => new RUDPMessageSender(
                        new DefaultMessageConverter(),
                        peer)));

            await onConnected(hub);
        }, CancellationToken.None);
    }

    private byte[] Sum_Requested(byte[] parameterData)
    {
        Sum_Parameter parameter = MessageSerializer.Deserialize(parameterData) as Sum_Parameter
                                 ?? throw new InvalidOperationException("Failed to deserialize Sum_Parameter.");
        float result = Sum_Implementation(parameter.a, parameter.b);
        return BitConverter.GetBytes(result);
    }
    private partial float Sum_Implementation(float a, float b);

    [Message]
    partial class Sum_Parameter
    {
        public float a;
        public float b;
        
        public Sum_Parameter(float a, float b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public string SumResultString(int a, int b)
    {
        throw new NotImplementedException();
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
