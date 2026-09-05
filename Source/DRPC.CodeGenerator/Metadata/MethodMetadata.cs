using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator.Metadata;

/// <summary>[RemoteProcedure] 가 붙은 계약 메서드 하나.</summary>
internal sealed class MethodMetadata
{
    public IMethodSymbol Symbol { get; }
    public string MethodName => Symbol.Name;

    /// <summary>타입 분류(메시형 여부)에 쓰는 이름 해석 테이블. 이미터가 그대로 투과한다.</summary>
    public AttributeReferences References { get; }

    /// <summary>선언 인터페이스 이름. 생성되는 페이로드 헬퍼 이름충돌 방지용 접두사.</summary>
    public string DeclarationName { get; }

    public int MethodId { get; }
    public bool HasExplicitMethodId { get; }
    public bool OneWay { get; }

    /// <summary>예: <c>global::DRPC.RpcDeliveryMode.Unreliable</c></summary>
    public string ModeExpression { get; }

    public ParameterMetadata[] Parameters { get; }
    public ITypeSymbol ReturnType { get; }

    public bool IsVoidReturn => ReturnType.SpecialType == SpecialType.System_Void;

    /// <summary>생성될 사용자 구현(partial) 메서드 서명.</summary>
    public string ImplementationSignature => IsVoidReturn
        ? $"global::System.Threading.Tasks.Task {MethodName}_Implementation({ParameterDeclarationList()})"
        : $"global::System.Threading.Tasks.Task<{ReturnTypeDisplay}> {MethodName}_Implementation({ParameterDeclarationList()})";

    /// <summary>생성된 코드 안에서 쓰는 반환 타입 표시(네임스페이스 차이 안전을 위해 항상 fully qualified).</summary>
    public string ReturnTypeDisplay => ReturnType.ToDisplayString(RpcPayload.Qualified);

    public MethodMetadata(IMethodSymbol methodSymbol, int ordinalMethodId, AttributeReferences references)
    {
        Symbol = methodSymbol;
        References = references;
        DeclarationName = methodSymbol.ContainingType?.Name ?? "Procedure";
        ReturnType = methodSymbol.ReturnType;
        Parameters = methodSymbol.Parameters
            .Select(p => new ParameterMetadata(p.Name, p.Type))
            .ToArray();

        AttributeData? attribute = methodSymbol.FindAttribute(references.RemoteProcedureAttributeType);
        ModeExpression = BuildModeExpression(attribute, references);
        (MethodId, HasExplicitMethodId) = ResolveMethodId(attribute, ordinalMethodId);
        OneWay = ResolveNamedFlag(attribute, nameof(OneWay));
    }

    public string ParameterDeclarationList() => string.Join(", ", Parameters.Select(p =>
        $"{p.Type.ToDisplayString(RpcPayload.Qualified)} {p.Name}"));

    static (int methodId, bool explicitId) ResolveMethodId(AttributeData? attribute, int ordinalMethodId)
    {
        if (attribute == null)
        {
            return (ordinalMethodId, false);
        }

        if (attribute.ConstructorArguments.Length >= 2 && attribute.ConstructorArguments[1].Value is int ctorId && ctorId >= 0)
        {
            return (ctorId, true);
        }

        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key == "MethodId" && named.Value.Value is int namedId && namedId >= 0)
            {
                return (namedId, true);
            }
        }

        return (ordinalMethodId, false);
    }

    static bool ResolveNamedFlag(AttributeData? attribute, string key)
    {
        if (attribute == null)
        {
            return false;
        }

        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key == key && named.Value.Value is bool flag)
            {
                return flag;
            }
        }

        return false;
    }

    static string BuildModeExpression(AttributeData? attribute, AttributeReferences references)
    {
        const string fallback = "global::DRPC.RpcDeliveryMode.ReliableOrdered";

        if (attribute == null || attribute.ConstructorArguments.Length == 0)
        {
            return fallback;
        }

        object? raw = attribute.ConstructorArguments[0].Value;
        if (raw == null)
        {
            return fallback;
        }

        int value = System.Convert.ToInt32(raw);
        INamedTypeSymbol? mode = references.RpcDeliveryModeType;
        if (mode != null)
        {
            foreach (ISymbol member in mode.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field &&
                    field.ConstantValue is int constant &&
                    constant == value)
                {
                    return $"global::DRPC.RpcDeliveryMode.{field.Name}";
                }
            }
        }

        return $"((global::DRPC.RpcDeliveryMode){value})";
    }

    internal sealed class ParameterMetadata
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }

        public ParameterMetadata(string name, ITypeSymbol type)
        {
            Name = name;
            Type = type;
        }
    }
}
