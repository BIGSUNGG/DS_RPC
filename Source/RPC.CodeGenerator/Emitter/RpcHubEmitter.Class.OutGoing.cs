using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Metadata;

namespace RPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitOutgoingProcedure(StringBuilder sb, MethodMetadata proc, string indent)
    {
        string syncReturn = proc.Return.Type.SpecialType == SpecialType.System_Void
            ? "void"
            : proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        string asyncReturn = proc.Return.Type.SpecialType == SpecialType.System_Void
            ? "global::System.Threading.Tasks.Task"
            : $"global::System.Threading.Tasks.Task<{proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>";

        var paramList = string.Join(", ", proc.Parameters.Select(p =>
            $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}"));

        if (proc.Return.Type.SpecialType == SpecialType.System_Void)
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
            if (proc.Return.IsMessage)
            {
                sb.AppendLine($"{indent}    return global::MessageProtocol.Serialize.MessageSerializer.Deserialize<{proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>(resultBytes);");
            }
            else
            {
                sb.AppendLine($"{indent}    {proc.ReturnMessageTypeName} resultPayload = MessageSerializer.Deserialize<{proc.ReturnMessageTypeName}>(resultBytes);");
                sb.AppendLine($"{indent}    return resultPayload.Value;");
            }
            sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine();

        if (proc.Return.Type.SpecialType == SpecialType.System_Void)
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
            if (proc.Return.IsMessage)
            {
                sb.AppendLine($"{indent}    return global::MessageProtocol.Serialize.MessageSerializer.Deserialize<{proc.Return.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>(resultBytes);");
            }
            else
            {
                sb.AppendLine($"{indent}    {proc.ReturnMessageTypeName} resultPayload = MessageSerializer.Deserialize<{proc.ReturnMessageTypeName}>(resultBytes);");
                sb.AppendLine($"{indent}    return resultPayload.Value;");
            }
            sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine();
    }

    static void EmitParameterPayloadSerialize(StringBuilder sb, string indent, MethodMetadata proc)
    {
        if (proc.Parameters.Length == 0)
        {
            sb.AppendLine($"{indent}byte[] parameterData = global::System.Array.Empty<byte>();");
            return;
        }

        if (proc.Parameters.Length == 1 && proc.Parameters[0].IsMessage)
        {
            sb.AppendLine($"{indent}byte[] parameterData = global::MessageProtocol.Serialize.MessageSerializer.Serialize<{proc.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>({proc.Parameters[0].Name});");
            return;
        }

        sb.AppendLine($"{indent}{proc.ParameterMessageTypeName} parameterPayload = new {proc.ParameterMessageTypeName}");
        sb.AppendLine($"{indent}{{");
        foreach (var parameter in proc.Parameters)
        {
            sb.AppendLine($"{indent}    {parameter.Name} = {parameter.Name},");
        }
        sb.AppendLine($"{indent}}};");
        sb.AppendLine($"{indent}byte[] parameterData = MessageSerializer.Serialize<{proc.ParameterMessageTypeName}>(parameterPayload);");
    }
}
