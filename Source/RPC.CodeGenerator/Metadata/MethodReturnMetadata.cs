using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Reference;

namespace RPC.CodeGenerator.Metadata;

internal sealed class MethodReturnMetadata
{
    public ITypeSymbol Type { get; }
    public bool IsMessage { get; }

    public MethodReturnMetadata(ITypeSymbol type, AttributeReferences references)
    {
        Type = type;
        IsMessage = type.IsMessage(references);
    }
}
