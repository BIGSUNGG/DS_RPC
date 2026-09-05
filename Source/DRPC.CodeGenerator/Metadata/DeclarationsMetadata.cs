using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator.Metadata;

/// <summary>계약 인터페이스(IServerProcedureDeclarations / IClientProcedureDeclarations)와 그 RPC 메서드 목록.</summary>
internal sealed class DeclarationsMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public MethodMetadata[] Methods { get; }

    public DeclarationsMetadata(INamedTypeSymbol declarationSymbol, AttributeReferences references)
    {
        Symbol = declarationSymbol;

        int ordinal = 0;
        var methods = new List<MethodMetadata>();

        foreach (IMethodSymbol method in declarationSymbol
                     .GetMembers()
                     .OfType<IMethodSymbol>()
                     .Where(static m => m.MethodKind == MethodKind.Ordinary)
                     .Where(static m => !m.IsImplicitlyDeclared)
                     .Where(m => m.FindAttribute(references.RemoteProcedureAttributeType) != null))
        {
            methods.Add(new MethodMetadata(method, ordinal++, references));
        }

        Methods = methods.ToArray();
    }
}
