using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;
using System.Linq;

namespace DRPC.CodeGenerator.Metadata;

internal sealed class MethodMetadata
{
    public IMethodSymbol Symbol { get; }
    public string MethodName => Symbol.Name;
    public int MethodId { get; }
    public bool OneWay { get; }
    public bool HasExplicitMethodId { get; }
    public MethodReturnMetadata Return { get; }
    public MethodParameterMetadata[] Parameters { get; }
    public string ReliableTypeExpression { get; }
    public string ParameterMessageTypeName => $"{MethodName}_Paramter";
    public string ReturnMessageTypeName => $"{MethodName}_Return";

    public MethodMetadata(IMethodSymbol methodSymbol, int ordinalMethodId, AttributeReferences references)
    {
        Symbol = methodSymbol;
        Return = new MethodReturnMetadata(methodSymbol.ReturnType, references);
        Parameters = methodSymbol.Parameters
            .Select(p => new MethodParameterMetadata(
                p.Name,
                p.Type,
                references))
            .ToArray();

        var attr = methodSymbol.FindAttribute(references.RemoteProcedureAttributeType);
        ReliableTypeExpression = BuildReliableTypeExpression(attr, references);
        (MethodId, HasExplicitMethodId) = ResolveMethodId(attr, ordinalMethodId);
        OneWay = ResolveOneWay(attr);
    }

    static (int methodId, bool explicitId) ResolveMethodId(AttributeData? attr, int ordinalMethodId)
    {
        if (attr == null)
        {
            return (ordinalMethodId, false);
        }

        if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int ctorId)
        {
            if (ctorId >= 0)
            {
                return (ctorId, true);
            }

            return (ordinalMethodId, false);
        }

        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "MethodId" && named.Value.Value is int namedId && namedId >= 0)
            {
                return (namedId, true);
            }
        }

        return (ordinalMethodId, false);
    }

    static bool ResolveOneWay(AttributeData? attr)
    {
        if (attr == null)
        {
            return false;
        }

        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "OneWay" && named.Value.Value is bool oneWay)
            {
                return oneWay;
            }
        }

        return false;
    }

    static string BuildReliableTypeExpression(AttributeData? attr, AttributeReferences references)
    {
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
