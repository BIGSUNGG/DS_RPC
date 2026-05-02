using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator.Metadata;

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
