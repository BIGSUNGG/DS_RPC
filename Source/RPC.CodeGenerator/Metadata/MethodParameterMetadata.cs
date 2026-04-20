using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Reference;

namespace RPC.CodeGenerator.Metadata;

internal sealed class MethodParameterMetadata
{
    public string Name { get; }
    public ITypeSymbol Type { get; }
    public bool IsMessage { get; }

    public MethodParameterMetadata(string name, ITypeSymbol type, AttributeReferences references)
    {
        Name = name;
        Type = type;
        IsMessage = type.IsMessage(references);
    }
}
