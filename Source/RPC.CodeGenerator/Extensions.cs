using System.Linq;
using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator;

internal static class Extensions
{
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
