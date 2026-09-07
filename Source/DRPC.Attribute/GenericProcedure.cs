namespace DRPC;

/// <summary>
/// <see cref="RemoteProcedure"/> 제네릭 메서드의 타입 파라미터 슬롯별 허용 타입을 사전 선언한다.
/// 선언되지 않은 타입 인자로 호출하면 컴파일(DRPCGEN008)·런타임 양쪽에서 에러가 난다.
/// </summary>
/// <example>
/// <code>
/// [RemoteProcedure(methodId: 7)]
/// [GenericProcedure(typeof(int), typeof(string))]          // T 슬롯 0
/// T GetDefault&lt;T&gt;();
///
/// [RemoteProcedure(methodId: 8)]
/// [GenericProcedure(0, typeof(int), typeof(string))]       // T1 슬롯
/// [GenericProcedure(1, typeof(float), typeof(double))]     // T2 슬롯
/// T1 Pick&lt;T1, T2&gt;(T2 low, T2 high);
///
/// [RemoteProcedure(methodId: 9)]
/// void Deliver&lt;T&gt;(Package&lt;T&gt; box);   // Package&lt;T&gt; 가 [GenericMessage] 구성을 선언했다면
///                                     // [GenericProcedure] 없이 구성에서 T 집합을 상속한다.
/// </code>
/// </example>
/// <remarks>
/// 허용 조합은 슬롯별 목록의 데카르트 곱이고, 와이어의 구성 인덱스는 선언 순서(슬롯 0이 가장 느리게 도는
/// 오도미터 순서)로 결정적 산출된다 — 목록 순서를 바꾸면 기존 피어와 호환이 깨진다.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GenericProcedureAttribute : System.Attribute
{
    /// <summary>타입 파라미터 순번(0 기반). 메서드의 N 번째 타입 파라미터에 대응한다.</summary>
    public int Slot { get; }

    /// <summary>이 슬롯에 허용되는 타입들. 순서가 와이어 구성 인덱스에 그대로 반영된다.</summary>
    public System.Type[] Types { get; }

    /// <summary>슬롯 0 선언. <c>[GenericProcedure(typeof(int), typeof(string))]</c></summary>
    public GenericProcedureAttribute(params System.Type[] types)
        : this(0, types)
    {
    }

    /// <summary>지정 슬롯 선언. <c>[GenericProcedure(1, typeof(float))]</c></summary>
    public GenericProcedureAttribute(int slot, params System.Type[] types)
    {
        if (types is null || types.Length == 0)
        {
            throw new System.ArgumentException("At least one type must be declared.", nameof(types));
        }

        Slot = slot;
        Types = types;
    }
}
