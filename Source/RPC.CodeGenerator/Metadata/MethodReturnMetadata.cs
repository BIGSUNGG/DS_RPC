using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator.Metadata;

internal sealed class MethodReturnMetadata
{
    public ITypeSymbol Type { get; }

    public MethodReturnMetadata(ITypeSymbol type)
    {
        Type = type;
    }
}
