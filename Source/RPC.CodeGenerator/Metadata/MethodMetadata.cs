using Microsoft.CodeAnalysis;
using RPC.CodeGenerator.Reference;
using System.Linq;

namespace RPC.CodeGenerator.Metadata;

internal sealed class MethodMetadata
{
    public IMethodSymbol Symbol { get; }
    public string MethodName { get; }
    public int MethodId { get; }
    public ITypeSymbol ReturnType { get; }
    public (string Name, ITypeSymbol Type)[] Parameters { get; }
    public string ReliableTypeExpression { get; }

    public MethodMetadata(IMethodSymbol methodSymbol, int methodId, AttributeReferences references)
    {
        Symbol = methodSymbol;
        MethodName = methodSymbol.Name;
        MethodId = methodId;
        ReturnType = methodSymbol.ReturnType;
        Parameters = methodSymbol.Parameters.Select(p => (p.Name, p.Type)).ToArray();
        ReliableTypeExpression = BuildReliableTypeExpression(methodSymbol, references);
    }

    static string BuildReliableTypeExpression(IMethodSymbol methodSymbol, AttributeReferences references)
    {
        var attr = methodSymbol.FindAttribute(references.RemoteProcedureAttributeType);
        if (attr == null || attr.ConstructorArguments.Length == 0)
        {
            return "global::Communication.Network.RUDP.Shared.Messages.ReliableType.ReliableOrdered";
        }

        int value = System.Convert.ToInt32(attr.ConstructorArguments[0].Value);
        var reliableEnum = references.ReliableTypeEnumType;
        if (reliableEnum != null)
        {
            foreach (var member in reliableEnum.GetMembers())
            {
                if (member is IFieldSymbol field &&
                    field.HasConstantValue &&
                    field.ConstantValue is int constantValue &&
                    constantValue == value &&
                    field.Name != "value__")
                {
                    return $"global::Communication.Network.RUDP.Shared.Messages.ReliableType.{field.Name}";
                }
            }
        }

        return $"((global::Communication.Network.RUDP.Shared.Messages.ReliableType){value})";
    }
}
