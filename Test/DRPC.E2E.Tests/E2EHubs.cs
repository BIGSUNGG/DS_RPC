using DRPC;
using DRPC.Client.Network;
using DRPC.Server.Network;
using DRPC.Shared.Interface;
using MessageProtocol;

namespace DRPC.E2E.Tests;

/// <summary>
/// RUDP 루프백 E2E 용 계약. 서버 계약(클라→서버)과 클라이언트 계약(서버→클라)을 한 파일에 둔다.
/// </summary>
public interface IServerProcedures : IServerProcedureDeclarations
{
    /// <summary>전송 방식 미지정 = 기본 ReliableOrdered.</summary>
    [RemoteProcedure(methodId: 0)]
    int Add(int value1, int value2);

    /// <summary>Sequenced 오버라이드(상태 갱신성 호출).</summary>
    [RemoteProcedure(RpcDeliveryMode.Sequenced, 1)]
    string Echo(string text);

    /// <summary>Unreliable 오버라이드.</summary>
    [RemoteProcedure(RpcDeliveryMode.Unreliable, 2)]
    void Ping(int seq);

    /// <summary>OneWay: 응답 없이 전달만.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 3, OneWay = true)]
    void Note(string text);

    /// <summary>메시지 타입(NonId) 매개변수·반환 + 중첩 컬렉션·decimal.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 4)]
    OrderSummary PlaceOrder(Order order);

    /// <summary>구현이 예외를 던지면 Unhandled 오류로 온다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 5)]
    int AlwaysFails();

    /// <summary>응답이 늦어 호출 측 타임아웃을 유발한다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 6)]
    int Slow(int delayMs);

    /// <summary>호출별 타임아웃(TimeoutMs=400): 허브 기본(30초)과 무관하게 이 호출만 빠르게 만료한다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 11, TimeoutMs = 400)]
    int SlowWithPerCallTimeout(int delayMs);

    /// <summary>제네릭 ①: 반환 전용 제네릭. 허용 T = int/string(메시지 타입 반환은 ③④ 가 담당).</summary>
    [RemoteProcedure(methodId: 7)]
    [GenericProcedure(typeof(int), typeof(string))]
    T GetDefault<T>();

    /// <summary>제네릭 ②: 매개변수 제네릭(호출 측 타입 추론). T = int/string.</summary>
    [RemoteProcedure(methodId: 8)]
    [GenericProcedure(typeof(int), typeof(string))]
    string Describe<T>(T value);

    /// <summary>제네릭 ③: 복합 다중 슬롯(반환 T1 + 매개변수 T2, T3, 데카르트 곱).</summary>
    [RemoteProcedure(methodId: 9)]
    [GenericProcedure(0, typeof(int), typeof(string))]
    [GenericProcedure(1, typeof(float), typeof(double))]
    [GenericProcedure(2, typeof(Order), typeof(ChatLine))]
    T1 Blend<T1, T2, T3>(T2 left, T3 right);

    /// <summary>제네릭 ④: [GenericMessage] 파라미터. T 허용 집합은 Package 구성 선언에서 상속(메시지 타입만 가능 — T 멤버는 런타임 메시지 디스패치로 직렬화된다).</summary>
    [RemoteProcedure(methodId: 10)]
    void Deliver<T>(Package<T> box);
}

public interface IClientProcedures : IClientProcedureDeclarations
{
    /// <summary>서버가 클라이언트로 역호출한다(양방향 RPC).</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 0)]
    int ClientValue();

    /// <summary>그룹 다형성: 실제 파생 타입이 보존된다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 1, OneWay = true)]
    void ReceiveLine(ChatLine line);
}

[NonIdMessage]
public partial class Order
{
    public string Item { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<int> Tags { get; set; } = new();
}

[NonIdMessage]
public partial class OrderSummary
{
    public string Receipt { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

[GroupRootMessage(20)]
public partial class ChatLine
{
    public string Text { get; set; } = string.Empty;
}

[GroupElementMessage(1)]
public partial class ShoutChatLine : ChatLine
{
}

/// <summary>제네릭 ④용 [GenericMessage]. 구성(ClassId)마다 와이어 (MessageId, ClassId) 로 식별된다.
/// T 는 ID 헤더 메시지(Standalone/Group)여야 한다 — NonId 는 제네릭 구성 등록이 막힌다.</summary>
[StandaloneMessage(50)]
[GenericMessage(typeof(Package<ChatLine>), ClassId = 1)]
[GenericMessage(typeof(Package<Receipt>), ClassId = 2)]
public partial class Package<T>
{
    public T Value { get; set; } = default!;
}

[StandaloneMessage(51)]
public partial class Receipt
{
    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// 서버 측 허브. Incoming = 서버 계약(자기가 구현), Outgoing = 클라이언트 계약(상대를 호출).
/// </summary>
public partial class E2EServerHub : ServerHub<IServerProcedures, IClientProcedures>
{
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> ReceivedNotes = new();
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> ReceivedGeneric = new();

    private partial Task<int> Add_Implementation(int value1, int value2) => Task.FromResult(value1 + value2);

    private partial Task<string> Echo_Implementation(string text) => Task.FromResult("echo:" + text);

    private partial Task Ping_Implementation(int seq) => Task.CompletedTask;

    private partial Task Note_Implementation(string text)
    {
        ReceivedNotes.Enqueue(text);
        return Task.CompletedTask;
    }

    private partial Task<OrderSummary> PlaceOrder_Implementation(Order order)
        => Task.FromResult(new OrderSummary
        {
            Receipt = $"{order.Item}x{order.Quantity}",
            Total = order.Quantity * 1.5m + order.Tags.Count,
        });

    private partial Task<int> AlwaysFails_Implementation() => throw new InvalidOperationException("intentional failure");

    private partial async Task<int> Slow_Implementation(int delayMs)
    {
        await Task.Delay(delayMs).ConfigureAwait(false);
        return delayMs;
    }

    private partial async Task<int> SlowWithPerCallTimeout_Implementation(int delayMs)
    {
        await Task.Delay(delayMs).ConfigureAwait(false);
        return delayMs;
    }

    private partial Task<T> GetDefault_Implementation<T>() => Task.FromResult<T>(default!);

    private partial Task<string> Describe_Implementation<T>(T value)
        => Task.FromResult($"{typeof(T).FullName}:{value}");

    private partial Task<T1> Blend_Implementation<T1, T2, T3>(T2 left, T3 right)
    {
        ReceivedGeneric.Enqueue($"Blend:{typeof(T2).Name}:{left?.GetType().Name ?? typeof(T2).Name}:{typeof(T3).Name}:{right?.GetType().Name ?? typeof(T3).Name}");
        return Task.FromResult<T1>(default!);
    }

    private partial Task Deliver_Implementation<T>(Package<T> box)
    {
        string detail = box.Value switch { ChatLine c => c.Text, Receipt r => r.Tag, _ => "?" };
        ReceivedGeneric.Enqueue($"Deliver:{typeof(T).Name}:{detail}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 클라이언트 측 허브. Outgoing = 서버 계약, Incoming = 클라이언트 계약.
/// 서버가 역호출하는 메서드만 여기서 구현한다.
/// </summary>
public partial class E2EClientHub : ClientHub<IServerProcedures, IClientProcedures>
{
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> ReceivedLines = new();

    private partial Task<int> ClientValue_Implementation() => Task.FromResult(4242);

    private partial Task ReceiveLine_Implementation(ChatLine line)
    {
        ReceivedLines.Enqueue(line.GetType().Name + ":" + line.Text);
        return Task.CompletedTask;
    }
}
