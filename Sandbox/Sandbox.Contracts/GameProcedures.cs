using DRPC;
using DRPC.Shared.Interface;
using MessageProtocol;

namespace Sandbox.Contracts;

/// <summary>
/// 서버가 구현하고 클라이언트가 호출하는 계약. 반환 타입은 <c>Task</c> 없이 plain 하게 쓴다
/// (생성된 스텁이 이미 <c>{Method}Async</c> 이다).
/// </summary>
public interface IGameServerProcedures : IServerProcedureDeclarations
{
    /// <summary>전송 방식 생략 = 기본 ReliableOrdered.</summary>
    [RemoteProcedure(methodId: 0)]
    int Add(int value1, int value2);

    /// <summary>MessageProtocol 메시지 타입([NonIdMessage])은 매개변수·반환 값으로 그대로 쓰면 된다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)]
    PlayerJoined Join(Player player);

    /// <summary>Sequenced 오버라이드: 유실 감수 + 최신 순서만 유지하는 상태 갱신.</summary>
    [RemoteProcedure(RpcDeliveryMode.Sequenced, 2)]
    void SetPosition(int playerId, float x, float y);

    /// <summary>OneWay: 응답을 기다리지 않는다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 3, OneWay = true)]
    void LogChat(string text);

    /// <summary>다형성: 그룹 루트 타입으로 선언하면 실제 타입으로 복원되어 전달된다.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 4, OneWay = true)]
    void ChatMessage(ChatLine line);
}

/// <summary>클라이언트가 구현하고 서버가 호출하는 계약(양방향 RPC).</summary>
public interface IGameClientProcedures : IClientProcedureDeclarations
{
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 0)]
    float EchoSum(List<float> values);

    /// <summary>nullable·배열 혼합 사용 예.</summary>
    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)]
    int CountConfig(string? label, int[] values);

    [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 2, OneWay = true)]
    void NotifyScore(ScoreBoard score);
}

/// <summary>DTO 는 MessageProtocol 메시지 표시를 붙인다 — RPC 는 이 직렬화를 그대로 재사용한다.</summary>
[NonIdMessage]
public partial class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[NonIdMessage]
public partial class PlayerJoined
{
    public int PlayerId { get; set; }
    public int RoomId { get; set; }
}

[NonIdMessage]
public partial class ScoreLine
{
    public int PlayerId { get; set; }
    public int Score { get; set; }
}

[NonIdMessage]
public partial class ScoreBoard
{
    public string Map { get; set; } = string.Empty;
    public List<ScoreLine> Lines { get; set; } = new();
}

/// <summary>그룹 루트. 이 타입을 매개변수로 받으면 아래 요소 타입들이 그대로 올라온다.</summary>
[GroupRootMessage(11)]
public partial class ChatLine
{
    public string Text { get; set; } = string.Empty;

    public virtual string Describe() => $"chat: {Text}";
}

[GroupElementMessage(0)]
public partial class ShoutChatLine : ChatLine
{
    public override string Describe() => $"SHOUT: {Text.ToUpperInvariant()}";
}
