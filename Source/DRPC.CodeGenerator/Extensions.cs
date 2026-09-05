using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator;

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

    /// <summary>
    /// 페이로드에 넣을 수 있는 메시지 타입인지. MessageProtocol 속성 또는 생성된
    /// <c>IMessageSerializable&lt;T&gt;</c> 구현 중 하나면 충분하다.
    /// </summary>
    public static bool IsMessage(this ITypeSymbol self, AttributeReferences references)
        => references.MessageStyleOf(self) != MessageStyle.None;
}
