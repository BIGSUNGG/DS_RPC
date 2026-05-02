using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;
using System.Linq;

namespace DRPC.CodeGenerator.Metadata;

internal sealed class MethodMetadata
{
    public IMethodSymbol Symbol { get; }
    public string MethodName => Symbol.Name;
    public uint MethodId { get; }
    public MethodReturnMetadata Return { get; }
    public MethodParameterMetadata[] Parameters { get; }
    public string ReliableTypeExpression { get; }
    public string ParameterMessageTypeName => $"{MethodName}_Paramter";
    public string ReturnMessageTypeName => $"{MethodName}_Return";

    public MethodMetadata(IMethodSymbol methodSymbol, uint methodId, AttributeReferences references)
    {
        Symbol = methodSymbol;
        MethodId = methodId;
        Return = new MethodReturnMetadata(methodSymbol.ReturnType, references);
        Parameters = methodSymbol.Parameters
            .Select(p => new MethodParameterMetadata(
                p.Name,
                p.Type,
                references))
            .ToArray();
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
