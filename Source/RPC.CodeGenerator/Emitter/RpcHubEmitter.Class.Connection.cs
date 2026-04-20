using System.Text;

namespace RPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitListenAsync(StringBuilder sb, string hubTypeName, string indent)
    {
        sb.AppendLine($"{indent}public static global::System.Threading.Tasks.Task ListenAsync(int port, global::System.Func<{hubTypeName}, global::System.Threading.Tasks.Task> onConnected, global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    return ListenAsync(port, \"\", onConnected, cancellationToken);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        sb.AppendLine($"{indent}public static async global::System.Threading.Tasks.Task ListenAsync(int port, string connectionKey, global::System.Func<{hubTypeName}, global::System.Threading.Tasks.Task> onConnected, global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    Listener listener = new(global::System.Net.IPAddress.Any, port, connectionKey);");
        sb.AppendLine($"{indent}    await listener.ListenAsync(async (peer, netManager, eventListener) =>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        {hubTypeName} hub = new(_ =>");
        sb.AppendLine($"{indent}            new ClientSession(");
        sb.AppendLine($"{indent}                peer,");
        sb.AppendLine($"{indent}                netManager,");
        sb.AppendLine($"{indent}                session => new RUDPMessageReceiver(");
        sb.AppendLine($"{indent}                    new DefaultMessageConverter(),");
        sb.AppendLine($"{indent}                    peer,");
        sb.AppendLine($"{indent}                    netManager,");
        sb.AppendLine($"{indent}                    eventListener,");
        sb.AppendLine($"{indent}                    new RPCMessageHandler(session, _)),");
        sb.AppendLine($"{indent}                __ => new RUDPMessageSender(");
        sb.AppendLine($"{indent}                    new DefaultMessageConverter(),");
        sb.AppendLine($"{indent}                    peer)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}        await onConnected(hub);");
        sb.AppendLine();
        sb.AppendLine($"{indent}        _ = global::System.Threading.Tasks.Task.Run(async () =>");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            while (!cancellationToken.IsCancellationRequested)");
        sb.AppendLine($"{indent}            {{");
        sb.AppendLine($"{indent}                netManager.PollEvents();");
        sb.AppendLine($"{indent}                await global::System.Threading.Tasks.Task.Delay(10);");
        sb.AppendLine($"{indent}            }}");
        sb.AppendLine($"{indent}        }}, cancellationToken);");
        sb.AppendLine($"{indent}    }}, cancellationToken);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    static void EmitConnectAsync(StringBuilder sb, string hubTypeName, string indent)
    {
        sb.AppendLine($"{indent}public static async global::System.Threading.Tasks.Task<{hubTypeName}> ConnectAsync(string host, int port, global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    return await ConnectAsync(host, port, \"\", cancellationToken);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        sb.AppendLine($"{indent}public static async global::System.Threading.Tasks.Task<{hubTypeName}> ConnectAsync(string host, int port, string connectionKey, global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    {hubTypeName}? connectedHub = null;");
        sb.AppendLine($"{indent}    Connector connector = new(host, port, connectionKey);");
        sb.AppendLine();
        sb.AppendLine($"{indent}    bool isConnected = await connector.ConnectAsync((peer, netManager, listener) =>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        connectedHub = new {hubTypeName}(hub =>");
        sb.AppendLine($"{indent}            new ServerSession(");
        sb.AppendLine($"{indent}                peer,");
        sb.AppendLine($"{indent}                netManager,");
        sb.AppendLine($"{indent}                session => new RUDPMessageReceiver(");
        sb.AppendLine($"{indent}                    new DefaultMessageConverter(),");
        sb.AppendLine($"{indent}                    peer,");
        sb.AppendLine($"{indent}                    netManager,");
        sb.AppendLine($"{indent}                    listener,");
        sb.AppendLine($"{indent}                    new RPCMessageHandler(session, hub)),");
        sb.AppendLine($"{indent}                _ => new RUDPMessageSender(");
        sb.AppendLine($"{indent}                    new DefaultMessageConverter(),");
        sb.AppendLine($"{indent}                    peer)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}        _ = global::System.Threading.Tasks.Task.Run(async () =>");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            while (!cancellationToken.IsCancellationRequested)");
        sb.AppendLine($"{indent}            {{");
        sb.AppendLine($"{indent}                netManager.PollEvents();");
        sb.AppendLine($"{indent}                await global::System.Threading.Tasks.Task.Delay(10);");
        sb.AppendLine($"{indent}            }}");
        sb.AppendLine($"{indent}        }}, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine($"{indent}        return global::System.Threading.Tasks.Task.CompletedTask;");
        sb.AppendLine($"{indent}    }});");
        sb.AppendLine();
        sb.AppendLine($"{indent}    if (isConnected == false || connectedHub == null)");
        sb.AppendLine($"{indent}        throw new global::System.InvalidOperationException(\"Failed to connect to server.\");");
        sb.AppendLine();
        sb.AppendLine($"{indent}    return connectedHub;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }
}
