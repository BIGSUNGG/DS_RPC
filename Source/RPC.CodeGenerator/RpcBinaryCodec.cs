using System.Linq;
using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator;

/// <summary>매개변수 바이너리 패킹(BinaryWriter/BinaryReader). MessageProtocol 생성기 순서와 무관하게 단일 패스 컴파일 가능.</summary>
internal static class RpcBinaryCodec
{
    public static void EmitSerializeParameters(System.Text.StringBuilder sb, string indent, (string Name, ITypeSymbol Type)[] parameters)
    {
        sb.AppendLine($"{indent}byte[] parameterData;");
        sb.AppendLine($"{indent}using (var __rpcMs = new global::System.IO.MemoryStream())");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    using (var __rpcW = new global::System.IO.BinaryWriter(__rpcMs))");
        sb.AppendLine($"{indent}    {{");
        foreach (var (name, type) in parameters)
        {
            sb.AppendLine($"{indent}        {EmitWriteLine(type, "__rpcW", name)}");
        }

        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    parameterData = __rpcMs.ToArray();");
        sb.AppendLine($"{indent}}}");
    }

    public static void EmitDeserializeLocals(
        System.Text.StringBuilder sb,
        string indent,
        (string Name, ITypeSymbol Type)[] parameters,
        string readerVar)
    {
        foreach (var (name, type) in parameters)
        {
            sb.AppendLine($"{indent}var {SanitizeLocal(name)} = {EmitReadExpression(readerVar, type)};");
        }
    }

    public static string BuildArgumentListFromLocals((string Name, ITypeSymbol Type)[] parameters)
    {
        return string.Join(", ", parameters.Select(p => SanitizeLocal(p.Name)));
    }

    static string SanitizeLocal(string name)
    {
        return name;
    }

    static string EmitWriteLine(ITypeSymbol type, string writerExpr, string valueExpr)
    {
        if (type.SpecialType == SpecialType.System_SByte)
        {
            return $"{writerExpr}.Write(unchecked((byte){valueExpr}));";
        }

        return $"{writerExpr}.Write({valueExpr});";
    }

    static string EmitReadExpression(string readerExpr, ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_SByte)
        {
            return $"unchecked((sbyte){readerExpr}.ReadByte())";
        }

        return $"{readerExpr}.{GetReadMethod(type)}()";
    }

    static string GetReadMethod(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                return "ReadBoolean";
            case SpecialType.System_Char:
                return "ReadChar";
            case SpecialType.System_Byte:
                return "ReadByte";
            case SpecialType.System_Int16:
                return "ReadInt16";
            case SpecialType.System_UInt16:
                return "ReadUInt16";
            case SpecialType.System_Int32:
                return "ReadInt32";
            case SpecialType.System_UInt32:
                return "ReadUInt32";
            case SpecialType.System_Int64:
                return "ReadInt64";
            case SpecialType.System_UInt64:
                return "ReadUInt64";
            case SpecialType.System_Single:
                return "ReadSingle";
            case SpecialType.System_Double:
                return "ReadDouble";
            case SpecialType.System_String:
                return "ReadString";
            default:
                return "ReadBoolean";
        }
    }
}
