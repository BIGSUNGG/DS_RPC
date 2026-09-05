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

/// <summary>
/// 서버 측 허브. Incoming = 서버 계약(자기가 구현), Outgoing = 클라이언트 계약(상대를 호출).
/// </summary>
public partial class E2EServerHub : ServerHub<IServerProcedures, IClientProcedures>
{
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> ReceivedNotes = new();

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
