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

    // DefaultMessageConverter는 HubSessionFactory.CreateDefaultConverter()로 대체
}
