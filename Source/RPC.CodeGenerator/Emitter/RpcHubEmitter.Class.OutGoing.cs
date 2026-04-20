using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Metadata;

namespace RPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitOutgoingProcedure(StringBuilder sb, MethodMetadata proc, string indent)
    {
        string syncReturn = proc.ReturnType.SpecialType == SpecialType.System_Void
            ? "void"
            : proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        string asyncReturn = proc.ReturnType.SpecialType == SpecialType.System_Void
            ? "global::System.Threading.Tasks.Task"
            : $"global::System.Threading.Tasks.Task<{proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>";

        var paramList = string.Join(", ", proc.Parameters.Select(p =>
            $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}"));

        if (proc.ReturnType.SpecialType == SpecialType.System_Void)
        {
            sb.AppendLine($"{indent}public void {proc.MethodName}({paramList})");
            sb.AppendLine($"{indent}{{");
            EmitParameterPayloadSerialize(sb, indent + "    ", proc);

            sb.AppendLine($"{indent}    global::System.Threading.Tasks.Task<byte[]> rpcTask = RequestRPC({proc.MethodId}, parameterData, {proc.ReliableTypeExpression});");
            sb.AppendLine($"{indent}    rpcTask.GetAwaiter().GetResult();");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            sb.AppendLine($"{indent}public {syncReturn} {proc.MethodName}({paramList})");
            sb.AppendLine($"{indent}{{");
            EmitParameterPayloadSerialize(sb, indent + "    ", proc);

            sb.AppendLine($"{indent}    global::System.Threading.Tasks.Task<byte[]> rpcTask = RequestRPC({proc.MethodId}, parameterData, {proc.ReliableTypeExpression});");
            sb.AppendLine($"{indent}    byte[] resultBytes = rpcTask.GetAwaiter().GetResult();");
            sb.AppendLine($"{indent}    return ({proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})MessageSerializer.Deserialize(resultBytes);");
            sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine();

        if (proc.ReturnType.SpecialType == SpecialType.System_Void)
        {
            sb.AppendLine($"{indent}public async {asyncReturn} {proc.MethodName}Async({paramList})");
            sb.AppendLine($"{indent}{{");
            EmitParameterPayloadSerialize(sb, indent + "    ", proc);

            sb.AppendLine($"{indent}    await RequestRPC({proc.MethodId}, parameterData, {proc.ReliableTypeExpression});");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            sb.AppendLine($"{indent}public async {asyncReturn} {proc.MethodName}Async({paramList})");
            sb.AppendLine($"{indent}{{");
            EmitParameterPayloadSerialize(sb, indent + "    ", proc);

            sb.AppendLine($"{indent}    byte[] resultBytes = await RequestRPC({proc.MethodId}, parameterData, {proc.ReliableTypeExpression});");
            sb.AppendLine($"{indent}    return ({proc.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})MessageSerializer.Deserialize(resultBytes);");
            sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine();
    }

    static void EmitParameterPayloadSerialize(StringBuilder sb, string indent, MethodMetadata proc)
    {
        if (proc.Parameters.Length == 0)
        {
            sb.AppendLine($"{indent}byte[] parameterData = MessageSerializer.Serialize(global::System.Array.Empty<object?>());");
            return;
        }

        sb.AppendLine($"{indent}object?[] parameterPayload = new object?[]");
        sb.AppendLine($"{indent}{{");
        foreach (var parameter in proc.Parameters)
        {
            sb.AppendLine($"{indent}    {parameter.Name},");
        }
        sb.AppendLine($"{indent}}};");
        sb.AppendLine($"{indent}byte[] parameterData = MessageSerializer.Serialize(parameterPayload);");
    }
}
