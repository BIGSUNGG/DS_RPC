using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator.Metadata;

internal sealed class MethodParameterMetadata
{
    public string Name { get; }
    public ITypeSymbol Type { get; }

    public MethodParameterMetadata(string name, ITypeSymbol type)
    {
        Name = name;
        Type = type;
    }
}
