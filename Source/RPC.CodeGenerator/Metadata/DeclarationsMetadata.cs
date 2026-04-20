using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Reference;
using System.Collections.Generic;
using System.Linq;

namespace RPC.CodeGenerator.Metadata;

internal sealed class DeclarationsMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public MethodMetadata[] Methods { get; }

    public DeclarationsMetadata(INamedTypeSymbol declarationSymbol, AttributeReferences references)
    {
        Symbol = declarationSymbol;

        uint methodId = 0;
        var methods = new List<MethodMetadata>();

        foreach (var method in declarationSymbol
                     .GetMembers()
                     .OfType<IMethodSymbol>()
                     .Where(m => m.MethodKind == MethodKind.Ordinary)
                     .Where(m => !m.IsImplicitlyDeclared)
                     .Where(m => m.FindAttribute(references.RemoteProcedureAttributeType) != null))
        {
            methods.Add(new MethodMetadata(method, methodId++, references));
        }

        Methods = methods.ToArray();
    }
}
