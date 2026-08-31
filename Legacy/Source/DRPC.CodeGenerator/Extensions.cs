using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator;

internal static class Extensions
{
    public static bool IsMessage(this ITypeSymbol self, AttributeReferences references)
    {
        return self.HasAttribute(references.MessageAttributeType)
               || self.HasAttribute(references.NonIdMessageAttributeType)
               || self.HasAttribute(references.GroupRootMessageAttributeType)
               || self.HasAttribute(references.GroupElementMessageAttributeType)
               || self.HasAttribute(references.StandaloneMessageAttributeType);
    }

    public static bool HasAttribute(this ISymbol self, INamedTypeSymbol? attributeSymbol)
    {
        if (attributeSymbol == null)
        {
            return false;
        }

        return self.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol));
    }

    public static AttributeData? FindAttribute(this ISymbol self, INamedTypeSymbol? attributeSymbol)
    {
        if (attributeSymbol == null)
        {
            return null;
        }

        foreach (var a in self.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol))
            {
                return a;
            }
        }

        return null;
    }
}
