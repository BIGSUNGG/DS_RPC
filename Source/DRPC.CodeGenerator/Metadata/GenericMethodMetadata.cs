using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator.Metadata;

/// <summary>
/// 제네릭 [RemoteProcedure] 메서드의 구성 메타데이터.
///
/// 슬롯별 허용 타입 목록([GenericProcedure])에서 데카르트 곱으로 닫힌 구성(instantiation)을
/// 결정적 순서(마지막 슬롯이 가장 빨리 도는 오도미터)로 산출한다. 이 순서가 곧 와이어의
/// 구성 인덱스(페이로드 첫 4바이트)이므로 양쪽 컴파일에서 같은 계약이면 같은 표가 나온다.
///
/// [GenericProcedure] 미선언 슬롯은 시그니처의 [GenericMessage] 파라미터·반환(예: Package&lt;T&gt;)
/// 의 구성 선언에서 허용 집합을 상속한다 — 이 경우 T 는 직접 직렬화되지 않고 메시지 헤더
/// (MessageId, ClassId) 가 T 를 식별한다.
/// </summary>
internal sealed class GenericMethodMetadata
{
    public IMethodSymbol Symbol { get; }

    /// <summary>슬롯(타입 파라미터 순번) → 허용 타입 목록(선언 순서).</summary>
    public IReadOnlyList<ITypeSymbol>?[] SlotTypes { get; }

    /// <summary>닫힌 구성 전체. 순서 = 와이어 구성 인덱스.</summary>
    public IReadOnlyList<ITypeSymbol[]> Instantiations { get; }

    /// <summary>구성별로 Construct 된 닫힌 메서드(매개변수·반환 타입 치환 완료).</summary>
    public IReadOnlyList<IMethodSymbol> ClosedMethods { get; }

    /// <summary>메서드 타입 파라미터 이름 목록(스텁·구현 partial 서명 표기용).</summary>
    public IReadOnlyList<string> TypeParameterNames { get; }

    /// <summary>검증 실패 사유. null 이면 유효.</summary>
    public string? Error { get; }

    /// <summary>실패 시 보고할 진단 ID(DRPCGEN007 미선언 / DRPCGEN009 선언 무효).</summary>
    public string ErrorId { get; } = "DRPCGEN009";

    /// <summary>생성기가 내보내는 구성 상한. 초과분은 진단으로 거부한다.</summary>
    public const int MaxInstantiations = 64;

    GenericMethodMetadata(
        IMethodSymbol symbol,
        IReadOnlyList<ITypeSymbol>?[] slotTypes,
        IReadOnlyList<ITypeSymbol[]> instantiations,
        IReadOnlyList<IMethodSymbol> closedMethods,
        string? error,
        string errorId = "DRPCGEN009")
    {
        ErrorId = errorId;
        Symbol = symbol;
        SlotTypes = slotTypes;
        Instantiations = instantiations;
        ClosedMethods = closedMethods;
        TypeParameterNames = symbol.TypeParameters.Select(static p => p.Name).ToArray();
        Error = error;
    }

    /// <summary>메서드 심볼에서 구성 메타데이터를 만든다. 오류 사유는 <see cref="Error"/> 로 돌려주고 예외는 던지지 않는다.</summary>
    public static GenericMethodMetadata Build(IMethodSymbol method, AttributeReferences references)
    {
        int arity = method.TypeParameters.Length;
        var slotTypes = new IReadOnlyList<ITypeSymbol>?[arity];

        // 1) [GenericProcedure] 선언 파싱
        foreach (var attribute in method.GetAttributes())
        {
            if (!references.IsGenericProcedureAttribute(attribute.AttributeClass))
            {
                continue;
            }

            // (params Type[]) → 슬롯 0, (int slot, params Type[]) → 지정 슬롯.
            int slot;
            var typeArgs = attribute.ConstructorArguments;
            if (typeArgs.Length == 1)
            {
                slot = 0;
            }
            else if (typeArgs.Length == 2 && typeArgs[0].Value is int declaredSlot)
            {
                slot = declaredSlot;
            }
            else
            {
                return Failed(method, "invalid [GenericProcedure] constructor shape", "DRPCGEN009");
            }

            if (slot < 0 || slot >= arity)
            {
                return Failed(method, $"[GenericProcedure] slot {slot} is out of range; the method has {arity} type parameter(s)", "DRPCGEN009");
            }

            if (slotTypes[slot] != null)
            {
                return Failed(method, $"[GenericProcedure] slot {slot} is declared more than once", "DRPCGEN009");
            }

            var types = typeArgs[^1].Values
                .Select(static v => v.Value)
                .OfType<ITypeSymbol>()
                .ToList();
            if (types.Count == 0)
            {
                return Failed(method, $"[GenericProcedure] slot {slot} declares no types", "DRPCGEN009");
            }

            if (types.Distinct(SymbolEqualityComparer.Default).Count() != types.Count)
            {
                return Failed(method, $"[GenericProcedure] slot {slot} declares the same type more than once", "DRPCGEN009");
            }

            slotTypes[slot] = types;
        }

        // 2) 미선언 슬롯: 시그니처의 [GenericMessage] 사용에서 상속(Package<T> 패턴).
        foreach (ITypeParameterSymbol typeParameter in method.TypeParameters)
        {
            int slot = typeParameter.Ordinal;
            if (slotTypes[slot] != null)
            {
                continue;
            }

            if (TryDeriveFromGenericMessage(method, typeParameter, references, out IReadOnlyList<ITypeSymbol>? derived, out string? deriveError))
            {
                slotTypes[slot] = derived;
            }
            else
            {
                return Failed(method, deriveError!, "DRPCGEN007");
            }
        }

        // 2.5) [GenericMessage] 타입 인자로 흐르는 슬롯은 MessageProtocol 런타임 메시지 디스패치로
        // 직렬화된다 — 허용 타입 전부가 메시지 타입이어야 한다(컴파일 타임 거부, 런타임 크래시 방지).
        foreach (ITypeParameterSymbol typeParameter in method.TypeParameters)
        {
            if (SlotUsedAsGenericMessageArgument(method, typeParameter, references) is not { } message)
            {
                continue;
            }

            foreach (ITypeSymbol allowed in slotTypes[typeParameter.Ordinal]!)
            {
                if (references.MessageStyleOf(allowed) != MessageStyle.HasId)
                {
                    return Failed(method,
                        $"type '{allowed.ToDisplayString()}' is used inside [GenericMessage] '{message.Name}' whose constructions are dispatched by ID header — declare an ID-header message type (Standalone/Group) instead",
                        "DRPCGEN009");
                }
            }
        }

        // 3) 데카르트 곱 + Construct.
        var instantiations = Cartesian(slotTypes!).ToList();
        if (instantiations.Count > MaxInstantiations)
        {
            return Failed(method, $"{instantiations.Count} instantiations exceed the cap of {MaxInstantiations}; reduce [GenericProcedure] types or slots", "DRPCGEN009");
        }

        var closed = instantiations
            .Select(args => method.Construct(args))
            .ToList();

        return new GenericMethodMetadata(method, slotTypes, instantiations, closed, null);
    }

    /// <summary>이 타입 파라미터가 [GenericMessage] 타입 인자 위치에 쓰였다면 그 메시지 선언을 돌려준다.</summary>
    static INamedTypeSymbol? SlotUsedAsGenericMessageArgument(IMethodSymbol method, ITypeParameterSymbol typeParameter, AttributeReferences references)
    {
        foreach (ITypeSymbol? usage in method.Parameters.Select(static p => p.Type)
                     .Append(method.ReturnType))
        {
            if (usage is INamedTypeSymbol named &&
                named.IsGenericType &&
                references.HasGenericMessageAttribute(named) &&
                named.TypeArguments.Any(t => SymbolEqualityComparer.Default.Equals(t, typeParameter)))
            {
                return named;
            }
        }

        return null;
    }

    /// <summary>Package&lt;T&gt; 처럼 슬롯이 [GenericMessage] 타입 인자로만 쓰이는 경우, 그 구성 선언에서 허용 집합을 얻는다.</summary>
    static bool TryDeriveFromGenericMessage(
        IMethodSymbol method,
        ITypeParameterSymbol typeParameter,
        AttributeReferences references,
        out IReadOnlyList<ITypeSymbol>? derived,
        out string? error)
    {
        derived = null;
        error = null;

        string slotName = typeParameter.Name;
        IReadOnlyList<ITypeSymbol>? found = null;
        int foundAtPosition = -1;
        INamedTypeSymbol? foundMessage = null;

        foreach (ITypeSymbol? usage in method.Parameters.Select(static p => p.Type)
                     .Append(method.ReturnType))
        {
            if (usage is not INamedTypeSymbol named || !named.IsGenericType)
            {
                continue;
            }

            if (!references.HasGenericMessageAttribute(named))
            {
                continue;
            }

            // 그 메시지의 구성 선언([GenericMessage(typeof(X&lt;타입&gt;), …)])을 읽는다.
            for (int position = 0; position < named.TypeArguments.Length; position++)
            {
                if (!SymbolEqualityComparer.Default.Equals(named.TypeArguments[position], typeParameter))
                {
                    continue;
                }

                if (found != null && (!SymbolEqualityComparer.Default.Equals(named, foundMessage) || position != foundAtPosition))
                {
                    error = $"type parameter '{slotName}' has no [GenericProcedure] declaration and is used in more than one [GenericMessage] position — declare [GenericProcedure] for slot {typeParameter.Ordinal} explicitly";
                    return false;
                }

                foundMessage = named;
                foundAtPosition = position;
                found = references.GetGenericConstructionArguments(named, position);
            }
        }

        if (found == null || found.Count == 0)
        {
            error = $"type parameter '{slotName}' has no [GenericProcedure] declaration and is not derivable from a [GenericMessage] parameter — declare [GenericProcedure({typeParameter.Ordinal}, …)]";
            return false;
        }

        derived = found;
        return true;
    }

    /// <summary>오도미터(마지막 슬롯이 가장 빨리 돈다). 슬롯 0 이 가장 느리게 도는 행 우선 순서.</summary>
    static IEnumerable<ITypeSymbol[]> Cartesian(IReadOnlyList<ITypeSymbol>[] slots)
    {
        int[] index = new int[slots.Length];
        long total = slots.Aggregate(1L, static (acc, s) => acc * s.Count);

        for (long n = 0; n < total; n++)
        {
            yield return slots.Select((s, k) => s[index[k]]).ToArray();

            for (int k = slots.Length - 1; k >= 0; k--)
            {
                if (++index[k] < slots[k].Count)
                {
                    break;
                }

                index[k] = 0;
            }
        }
    }

    static GenericMethodMetadata Failed(IMethodSymbol method, string error, string errorId)
        => new(method, new IReadOnlyList<ITypeSymbol>?[method.TypeParameters.Length],
            System.Array.Empty<ITypeSymbol[]>(), System.Array.Empty<IMethodSymbol>(), error, errorId);
}
