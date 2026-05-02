using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Metadata;

namespace DRPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitRpcMessageTypes(StringBuilder sb, TypeMetadata typeMeta, string indent)
    {
        var methods = typeMeta.Outgoing
            .Concat(typeMeta.Incoming)
            .GroupBy(static m => m.MethodName)
            .Select(static g => g.First());

        foreach (var method in methods)
        {
            bool needParameterWrapper = method.Parameters.Length > 0;
            if (needParameterWrapper)
            {
                EmitParameterMessageType(sb, method, indent);
            }

            bool needReturnWrapper = method.Return.Type.SpecialType != SpecialType.System_Void;
            if (needReturnWrapper)
            {
                EmitReturnMessageType(sb, method, indent);
            }
        }
    }

    static void EmitParameterMessageType(StringBuilder sb, MethodMetadata method, string indent)
    {
        sb.AppendLine($"{indent}[global::MessageProtocol.NonIdMessage]");
        sb.AppendLine($"{indent}public partial class {method.ParameterMessageTypeName}");
        sb.AppendLine($"{indent}{{");

        foreach (var parameter in method.Parameters)
        {
            string initializer = parameter.Type.IsReferenceType ? " = default!;" : string.Empty;
            sb.AppendLine($"{indent}    public {parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {parameter.Name} {{ get; set; }}{initializer}");
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    static void EmitReturnMessageType(StringBuilder sb, MethodMetadata method, string indent)
    {
        sb.AppendLine($"{indent}[global::MessageProtocol.NonIdMessage]");
        sb.AppendLine($"{indent}public partial class {method.ReturnMessageTypeName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    public {method.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} Value {{ get; set; }} = default!;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

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
