using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DRPC.CodeGenerator.Metadata;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator;

/// <summary>
/// 제네릭 스텁 호출 지점 검사(DRPCGEN008). 생성된 <c>{Method}Async&lt;…&gt;</c> 스텁은 입력 컴파일에
/// 아직 없으므로 심볼이 못 풀리는(미해결) 호출을 구조적으로 잡는다: 리시버가 허브이고 이름이
/// 계약 제네릭 메서드 + "Async" 이면, 명시 타입 인자 또는 인자 타입 추론으로 슬롯 집합을 검증한다.
/// 추론이 안 되는 자리(반환 전용 슬롯 등)는 스킵 — 런타임 백스톱(stub throw)이 남아 있다.
/// </summary>
internal static class GenericCallSiteCheck
{
    public static Diagnostic? Check(InvocationExpressionSyntax invocation, SemanticModel semanticModel, AttributeReferences references)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name: SimpleNameSyntax name } access)
        {
            return null;
        }

        string invokedName = name.Identifier.ValueText;
        if (!invokedName.EndsWith("Async", StringComparison.Ordinal))
        {
            return null;
        }

        // 사용자가 직접 정의한 멤버로 이미 풀리면 생성 스텁 호출이 아니다.
        if (semanticModel.GetSymbolInfo(invocation).Symbol != null)
        {
            return null;
        }

        if (semanticModel.GetTypeInfo(access.Expression).Type is not INamedTypeSymbol receiverType)
        {
            return null;
        }

        if (!RpcHubSourceGenerator.TryResolveHub(receiverType, out INamedTypeSymbol? hubBase, out _, out _))
        {
            return null;
        }

        string contractMethodName = invokedName[..^"Async".Length];

        foreach (INamedTypeSymbol? contract in new[] { hubBase!.TypeArguments[0] as INamedTypeSymbol, hubBase.TypeArguments[1] as INamedTypeSymbol })
        {
            if (contract == null)
            {
                continue;
            }

            var declarations = new DeclarationsMetadata(contract, references);
            MethodMetadata? method = declarations.Methods.FirstOrDefault(m => m.MethodName == contractMethodName);

            if (method?.Generic is not { Error: null } generic)
            {
                continue;
            }

            ITypeSymbol?[] bindings = new ITypeSymbol?[generic.SlotTypes.Length];

            // 명시 타입 인자: <int, string>
            if (name is GenericNameSyntax genericName &&
                genericName.TypeArgumentList.Arguments.Count == bindings.Length)
            {
                for (int k = 0; k < bindings.Length; k++)
                {
                    bindings[k] = semanticModel.GetSymbolInfo(genericName.TypeArgumentList.Arguments[k]).Symbol as ITypeSymbol;
                }
            }
            else if (invocation.ArgumentList.Arguments.Count == method.Parameters.Length)
            {
                // 추론: 매개변수 타입이 그대로 타입 파라미터이거나 [GenericMessage] 단일 위치 사용이면 인자에서 묶는다.
                for (int j = 0; j < method.Parameters.Length; j++)
                {
                    ITypeSymbol parameterType = method.Parameters[j].Type;
                    ITypeSymbol? argumentType = semanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[j].Expression).Type;

                    BindSlot(parameterType, argumentType, method, generic, bindings);
                }
            }

            for (int k = 0; k < bindings.Length; k++)
            {
                if (bindings[k] is not { } bound)
                {
                    continue;
                }

                if (!generic.SlotTypes[k]!.Any(allowed => SymbolEqualityComparer.Default.Equals(allowed, bound)))
                {
                    Location location = (name as GenericNameSyntax)?.TypeArgumentList.GetLocation()
                        ?? invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression.GetLocation()
                        ?? invocation.GetLocation();

                    return Diagnostic.Create(DiagnosticDescriptors.GenericTypeArgumentNotDeclared, location,
                        contractMethodName, bound.ToDisplayString(), contractMethodName, k);
                }
            }

            return null;
        }

        return null;
    }

    /// <summary>매개변수 타입이 (a) 타입 파라미터 그 자체이거나 (b) 그 타입 파라미터를 인자로 쓰는 [GenericMessage] 면 인자 타입에서 슬롯을 묶는다.</summary>
    static void BindSlot(ITypeSymbol parameterType, ITypeSymbol? argumentType, MethodMetadata method, GenericMethodMetadata generic, ITypeSymbol?[] bindings)
    {
        if (argumentType == null)
        {
            return;
        }

        for (int k = 0; k < method.Symbol.TypeParameters.Length; k++)
        {
            ITypeParameterSymbol typeParameter = method.Symbol.TypeParameters[k];

            if (parameterType is ITypeParameterSymbol direct &&
                SymbolEqualityComparer.Default.Equals(direct, typeParameter))
            {
                bindings[k] = argumentType;
                return;
            }

            if (parameterType is INamedTypeSymbol message &&
                message.IsGenericType &&
                method.References.HasGenericMessageAttribute(message) &&
                message.TypeArguments.Any(t => SymbolEqualityComparer.Default.Equals(t, typeParameter)) &&
                argumentType is INamedTypeSymbol closed &&
                SymbolEqualityComparer.Default.Equals(closed.ConstructedFrom, message.ConstructedFrom))
            {
                for (int m = 0; m < message.TypeArguments.Length; m++)
                {
                    if (SymbolEqualityComparer.Default.Equals(message.TypeArguments[m], typeParameter))
                    {
                        bindings[k] = closed.TypeArguments.Length == message.TypeArguments.Length ? closed.TypeArguments[m] : null;
                        return;
                    }
                }
            }
        }
    }
}
