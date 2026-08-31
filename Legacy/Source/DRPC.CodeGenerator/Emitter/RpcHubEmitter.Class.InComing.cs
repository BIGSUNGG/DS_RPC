using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Metadata;

namespace DRPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitIncomingProcedure(StringBuilder sb, MethodMetadata proc, string indent)
    {
        sb.AppendLine($"{indent}private async global::System.Threading.Tasks.Task<byte[]> {proc.MethodName}_Requested(byte[] parameterData)");
        sb.AppendLine($"{indent}{{");
        if (proc.Parameters.Length == 0)
        {
            if (proc.Return.Type.SpecialType == SpecialType.System_Void)
            {
                sb.AppendLine($"{indent}    await {proc.MethodName}_Implementation().ConfigureAwait(false);");
                sb.AppendLine($"{indent}    return global::System.Array.Empty<byte>();");
            }
            else
            {
                sb.AppendLine($"{indent}    {proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} result = await {proc.MethodName}_Implementation().ConfigureAwait(false);");
                sb.AppendLine($"{indent}    {proc.ReturnMessageTypeName} resultPayload = new {proc.ReturnMessageTypeName} {{ Value = result }};");
                sb.AppendLine($"{indent}    return MessageSerializer.Serialize<{proc.ReturnMessageTypeName}>(resultPayload);");
            }
        }
        else
        {
            sb.AppendLine($"{indent}    {proc.ParameterMessageTypeName} parameterPayload = MessageSerializer.Deserialize<{proc.ParameterMessageTypeName}>(parameterData);");

            string args = BuildArgumentListFromPayload(proc);
            if (proc.Return.Type.SpecialType == SpecialType.System_Void)
            {
                sb.AppendLine($"{indent}    await {proc.MethodName}_Implementation({args}).ConfigureAwait(false);");
                sb.AppendLine($"{indent}    return global::System.Array.Empty<byte>();");
            }
            else
            {
                sb.AppendLine(
                    $"{indent}    {proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} result = await {proc.MethodName}_Implementation({args}).ConfigureAwait(false);");
                sb.AppendLine($"{indent}    {proc.ReturnMessageTypeName} resultPayload = new {proc.ReturnMessageTypeName} {{ Value = result }};");
                sb.AppendLine($"{indent}    return MessageSerializer.Serialize<{proc.ReturnMessageTypeName}>(resultPayload);");
            }
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        if (proc.Return.Type.SpecialType == SpecialType.System_Void)
        {
            sb.AppendLine($"{indent}private partial global::System.Threading.Tasks.Task {proc.MethodName}_Implementation({ParameterDeclarationList(proc)});");
        }
        else
        {
            string ret = proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            sb.AppendLine($"{indent}private partial global::System.Threading.Tasks.Task<{ret}> {proc.MethodName}_Implementation({ParameterDeclarationList(proc)});");
        }

        sb.AppendLine();
    }

    static string ParameterDeclarationList(MethodMetadata proc)
    {
        return string.Join(", ", proc.Parameters.Select(p =>
            $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}"));
    }

    static string BuildArgumentListFromPayload(MethodMetadata proc)
    {
        return string.Join(", ", proc.Parameters.Select(p =>
            $"parameterPayload.{p.Name}"));
    }
}
