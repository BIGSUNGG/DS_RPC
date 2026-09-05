using System.Linq;
using System.Text;
using DRPC.CodeGenerator.Metadata;

namespace DRPC.CodeGenerator.Emitter;

/// <summary>
/// 페이로드 헬퍼 렌더링. 매개변수와 반환 값을 MessageBufferWriter/Reader 로 이어 붙인다.
/// </summary>
internal static partial class RpcHubEmitter
{
    const string BufferCreate = "global::MessageProtocol.Serialize.MessageBufferWriter.Create(64)";
    const string ReaderCtor = "new global::MessageProtocol.Serialize.MessageBufferReader(__data)";

    /// <summary>호출부에서 쓰는 인라인 페이로드 쓰기. 매개변수가 없으면 빈 배열.</summary>
    static void EmitPayloadWrite(StringBuilder sb, string indent, MethodMetadata method, string arguments, string target)
    {
        if (method.Parameters.Length == 0)
        {
            sb.AppendLine($"{indent}byte[] {target} = global::System.Array.Empty<byte>();");
            return;
        }

        sb.AppendLine($"{indent}byte[] {target} = {WriteParams(method)}({arguments});");
    }

    /// <summary>메서드 하나분 페이로드 헬퍼(매개변수 쓰/읽기, 반환 쓰/읽기).</summary>
    static void EmitPayloadHelpers(StringBuilder sb, MethodMetadata method, string indent)
    {
        if (method.Parameters.Length > 0)
        {
            sb.AppendLine($"{indent}static byte[] {WriteParams(method)}({method.ParameterDeclarationList()})");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var __buf = {BufferCreate};");
            sb.AppendLine($"{indent}    try");
            sb.AppendLine($"{indent}    {{");
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                RpcPayload.EmitWrite(sb, indent + "        ", method.Parameters[i].Type, method.Parameters[i].Name,
                    method.References, depth: i + 1);
            }

            sb.AppendLine($"{indent}        return __buf.ToArray();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    finally");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        __buf.Dispose();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            sb.AppendLine($"{indent}static void {ReadParams(method)}(byte[] __data, {OutParameters(method)})");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var __rd = {ReaderCtor};");
            foreach (var parameter in method.Parameters)
            {
                sb.AppendLine($"{indent}    {parameter.Name} = {DefaultLiteral(parameter.Type)};");
            }

            for (int i = 0; i < method.Parameters.Length; i++)
            {
                RpcPayload.EmitRead(sb, indent + "    ", method.Parameters[i].Type, method.Parameters[i].Name,
                    declare: false, method.References, depth: i + 1);
            }

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        if (!method.IsVoidReturn)
        {
            string returnType = method.ReturnTypeDisplay;
            sb.AppendLine($"{indent}static byte[] {WriteReturn(method)}({returnType} __value)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var __buf = {BufferCreate};");
            sb.AppendLine($"{indent}    try");
            sb.AppendLine($"{indent}    {{");
            RpcPayload.EmitWrite(sb, indent + "        ", method.ReturnType, "__value", method.References, depth: 1);
            sb.AppendLine($"{indent}        return __buf.ToArray();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    finally");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        __buf.Dispose();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            sb.AppendLine($"{indent}static {returnType} {ReadReturn(method)}(byte[] __data)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var __rd = {ReaderCtor};");
            RpcPayload.EmitRead(sb, indent + "    ", method.ReturnType, "__result", declare: true, method.References, depth: 1);
            sb.AppendLine($"{indent}    return __result;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

    static string OutParameters(MethodMetadata method) => string.Join(", ", method.Parameters.Select(p =>
        $"out {p.Type.ToDisplayString(RpcPayload.Qualified)} {p.Name}"));

    static string DefaultLiteral(Microsoft.CodeAnalysis.ITypeSymbol type)
        => type.IsReferenceType ? "default!" : "default";
}
