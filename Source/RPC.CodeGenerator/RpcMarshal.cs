using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator;

internal static class RpcMarshal
{
    public static bool IsSupportedDataType(ITypeSymbol type, bool allowVoid, out string display)
    {
        display = type.ToDisplayString();
        if (allowVoid && type.SpecialType == SpecialType.System_Void)
        {
            return true;
        }

        if (!allowVoid && type.SpecialType == SpecialType.System_Void)
        {
            return false;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
                return true;
            default:
                return IsEnumerableType(type);
        }
    }

    public static string EmitDecodeReturnExpression(ITypeSymbol returnType, string bytesVar = "resultBytes")
    {
        switch (returnType.SpecialType)
        {
            case SpecialType.System_Boolean:
                return $"global::System.BitConverter.ToBoolean({bytesVar}, 0)";
            case SpecialType.System_Char:
                return $"global::System.BitConverter.ToChar({bytesVar}, 0)";
            case SpecialType.System_SByte:
                return $"unchecked((sbyte){bytesVar}[0])";
            case SpecialType.System_Byte:
                return $"{bytesVar}[0]";
            case SpecialType.System_Int16:
                return $"global::System.BitConverter.ToInt16({bytesVar}, 0)";
            case SpecialType.System_UInt16:
                return $"global::System.BitConverter.ToUInt16({bytesVar}, 0)";
            case SpecialType.System_Int32:
                return $"global::System.BitConverter.ToInt32({bytesVar}, 0)";
            case SpecialType.System_UInt32:
                return $"global::System.BitConverter.ToUInt32({bytesVar}, 0)";
            case SpecialType.System_Int64:
                return $"global::System.BitConverter.ToInt64({bytesVar}, 0)";
            case SpecialType.System_UInt64:
                return $"global::System.BitConverter.ToUInt64({bytesVar}, 0)";
            case SpecialType.System_Single:
                return $"global::System.BitConverter.ToSingle({bytesVar}, 0)";
            case SpecialType.System_Double:
                return $"global::System.BitConverter.ToDouble({bytesVar}, 0)";
            case SpecialType.System_String:
                return $"RpcDecodeString({bytesVar})";
            default:
                return "default!";
        }
    }

    public static string EmitEncodeReturnExpression(ITypeSymbol returnType, string valueExpr)
    {
        switch (returnType.SpecialType)
        {
            case SpecialType.System_Void:
                return "global::System.Array.Empty<byte>()";
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                return $"global::System.BitConverter.GetBytes({valueExpr})";
            case SpecialType.System_SByte:
                return $"new byte[] {{ unchecked((byte){valueExpr}) }}";
            case SpecialType.System_Byte:
                return $"new byte[] {{ {valueExpr} }}";
            case SpecialType.System_String:
                return $"RpcEncodeString({valueExpr})";
            default:
                return "global::System.Array.Empty<byte>()";
        }
    }

    static bool IsEnumerableType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                return true;
            }

            if (iface is INamedTypeSymbol named &&
                named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return true;
            }
        }

        return false;
    }
}
