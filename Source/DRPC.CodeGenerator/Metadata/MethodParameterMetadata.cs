using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator.Metadata;

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
