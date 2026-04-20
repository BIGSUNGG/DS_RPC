using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Metadata;

namespace RPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitIncomingProcedure(StringBuilder sb, MethodMetadata proc, string indent)
    {
        sb.AppendLine($"{indent}private byte[] {proc.MethodName}_Requested(byte[] parameterData)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    {proc.ParameterMessageTypeName} parameterPayload = MessageSerializer.Deserialize<{proc.ParameterMessageTypeName}>(parameterData);");

        if (proc.Parameters.Length == 0)
        {
            if (proc.ReturnType.SpecialType == SpecialType.System_Void)
            {
                sb.AppendLine($"{indent}    {proc.MethodName}_Implementation();");
                sb.AppendLine($"{indent}    return MessageSerializer.Serialize<{proc.ReturnMessageTypeName}>(new {proc.ReturnMessageTypeName}());");
            }
            else
            {
                sb.AppendLine($"{indent}    {proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} result = {proc.MethodName}_Implementation();");
                sb.AppendLine($"{indent}    {proc.ReturnMessageTypeName} resultPayload = new {proc.ReturnMessageTypeName} {{ Value = result }};");
                sb.AppendLine($"{indent}    return MessageSerializer.Serialize<{proc.ReturnMessageTypeName}>(resultPayload);");
            }
        }
        else
        {
            string args = BuildArgumentListFromPayload(proc);
            if (proc.ReturnType.SpecialType == SpecialType.System_Void)
            {
                sb.AppendLine($"{indent}    {proc.MethodName}_Implementation({args});");
                sb.AppendLine($"{indent}    return MessageSerializer.Serialize<{proc.ReturnMessageTypeName}>(new {proc.ReturnMessageTypeName}());");
            }
            else
            {
                sb.AppendLine(
                    $"{indent}    {proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} result = {proc.MethodName}_Implementation({args});");
                sb.AppendLine($"{indent}    {proc.ReturnMessageTypeName} resultPayload = new {proc.ReturnMessageTypeName} {{ Value = result }};");
                sb.AppendLine($"{indent}    return MessageSerializer.Serialize<{proc.ReturnMessageTypeName}>(resultPayload);");
            }
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        string partialKeyword = proc.ReturnType.SpecialType == SpecialType.System_Void ? "partial void" : $"partial {proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}";
        sb.AppendLine($"{indent}private {partialKeyword} {proc.MethodName}_Implementation({ParameterDeclarationList(proc)});");
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
