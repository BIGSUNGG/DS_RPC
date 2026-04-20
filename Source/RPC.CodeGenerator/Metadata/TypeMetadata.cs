using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator.Metadata;

internal sealed class TypeMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public MethodMetadata[] Methods { get; }
    public DeclarationsMetadata Declarations { get; }

    public TypeMetadata(INamedTypeSymbol typeSymbol, DeclarationsMetadata declarations)
    {
        Symbol = typeSymbol;
        Declarations = declarations;
        Methods = declarations.Methods;
    }
}
