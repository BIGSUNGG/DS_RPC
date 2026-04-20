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
