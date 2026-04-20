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

        if (proc.Parameters.Length == 0)
        {
            if (proc.ReturnType.SpecialType == SpecialType.System_Void)
            {
                sb.AppendLine($"{indent}    {proc.MethodName}_Implementation();");
                sb.AppendLine($"{indent}    return global::System.Array.Empty<byte>();");
            }
            else
            {
                sb.AppendLine($"{indent}    {proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} result = {proc.MethodName}_Implementation();");
                sb.AppendLine($"{indent}    return {RpcMarshal.EmitEncodeReturnExpression(proc.ReturnType, "result")};");
            }
        }
        else
        {
            sb.AppendLine($"{indent}    using (var __rpcMs = new global::System.IO.MemoryStream(parameterData))");
            sb.AppendLine($"{indent}    using (var __rpcR = new global::System.IO.BinaryReader(__rpcMs))");
            sb.AppendLine($"{indent}    {{");
            RpcBinaryCodec.EmitDeserializeLocals(sb, indent + "        ", proc.Parameters, "__rpcR");
            string args = RpcBinaryCodec.BuildArgumentListFromLocals(proc.Parameters);
            if (proc.ReturnType.SpecialType == SpecialType.System_Void)
            {
                sb.AppendLine($"{indent}        {proc.MethodName}_Implementation({args});");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    return global::System.Array.Empty<byte>();");
            }
            else
            {
                sb.AppendLine(
                    $"{indent}        {proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} result = {proc.MethodName}_Implementation({args});");
                sb.AppendLine($"{indent}        return {RpcMarshal.EmitEncodeReturnExpression(proc.ReturnType, "result")};");
                sb.AppendLine($"{indent}    }}");
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
}
