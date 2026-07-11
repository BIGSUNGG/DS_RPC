using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;
using System.Collections.Generic;
using System.Linq;

namespace DRPC.CodeGenerator.Metadata;

internal sealed class DeclarationsMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public MethodMetadata[] Methods { get; }

    public DeclarationsMetadata(INamedTypeSymbol declarationSymbol, AttributeReferences references)
    {
        Symbol = declarationSymbol;

        int ordinal = 0;
        var methods = new List<MethodMetadata>();

        foreach (var method in declarationSymbol
                     .GetMembers()
                     .OfType<IMethodSymbol>()
                     .Where(m => m.MethodKind == MethodKind.Ordinary)
                     .Where(m => !m.IsImplicitlyDeclared)
                     .Where(m => m.FindAttribute(references.RemoteProcedureAttributeType) != null))
        {
            methods.Add(new MethodMetadata(method, ordinal++, references));
        }

        Methods = methods.ToArray();
    }
}
