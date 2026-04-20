using System.Text;
using RPC.CodeGenerator.Metadata;

namespace RPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    public static string Emit(TypeMetadata model)
    {
        var sb = new StringBuilder();
        EmitHeader(sb);

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
        }

        string indent = string.IsNullOrEmpty(model.Namespace) ? "" : "    ";
        EmitPartialClass(sb, model, indent);

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }
}
