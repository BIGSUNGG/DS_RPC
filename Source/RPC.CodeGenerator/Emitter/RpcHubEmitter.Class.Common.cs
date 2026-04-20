using System.Text;

namespace RPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitDefaultMessageConverter(StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}private sealed class DefaultMessageConverter : Communication.Shared.Messages.IMessageConverter");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    public byte[] Serialize(object message)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return MessageSerializer.Serialize(message);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}    public object Deserialize(global::System.ReadOnlySpan<byte> messageData)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return MessageSerializer.Deserialize(messageData.ToArray());");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    static void EmitStringHelpers(StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}private static byte[] RpcEncodeString(string value)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    value ??= string.Empty;");
        sb.AppendLine($"{indent}    var bytes = global::System.Text.Encoding.UTF8.GetBytes(value);");
        sb.AppendLine($"{indent}    var buf = new byte[4 + bytes.Length];");
        sb.AppendLine($"{indent}    global::System.Buffer.BlockCopy(global::System.BitConverter.GetBytes(bytes.Length), 0, buf, 0, 4);");
        sb.AppendLine($"{indent}    global::System.Buffer.BlockCopy(bytes, 0, buf, 4, bytes.Length);");
        sb.AppendLine($"{indent}    return buf;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        sb.AppendLine($"{indent}private static string RpcDecodeString(byte[] data)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (data == null || data.Length < 4) return string.Empty;");
        sb.AppendLine($"{indent}    int len = global::System.BitConverter.ToInt32(data, 0);");
        sb.AppendLine($"{indent}    if (len <= 0 || 4 + len > data.Length) return string.Empty;");
        sb.AppendLine($"{indent}    return global::System.Text.Encoding.UTF8.GetString(data, 4, len);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }
}
