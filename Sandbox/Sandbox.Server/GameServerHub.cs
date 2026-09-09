using DRPC.Server.Network;
using Sandbox.Contracts;

namespace Sandbox.Server;

/// <summary>
/// 서버 측 허브. 계약 메서드마다 <c>{Name}_Implementation</c> partial 만 채우면 된다 —
/// 전송·직렬화·CallId·라우팅은 생성된 코드가 처리한다.
/// </summary>
public partial class GameServerHub : ServerHub<IGameServerProcedures, IGameClientProcedures>
{
    /// <summary>[RemoteProcedure] 만 붙인 선언 = 기본값 ReliableOrdered.</summary>
    private partial Task<int> Add_Implementation(int value1, int value2)
        => Task.FromResult(value1 + value2);

    private partial Task<PlayerJoined> Join_Implementation(Player player)
    {
        Console.WriteLine($"[server] Join from player {player.Id} ({player.Name})");
        return Task.FromResult(new PlayerJoined { PlayerId = player.Id, RoomId = 100 });
    }

    /// <summary>Sequenced 로 들어오는 상태 갱신(유실·순서 역전 감수).</summary>
    private partial Task SetPosition_Implementation(int playerId, float x, float y)
    {
        Console.WriteLine($"[server] SetPosition player={playerId} pos=({x}, {y})");
        return Task.CompletedTask;
    }

    /// <summary>OneWay 이라 응답을 보내지 않는다.</summary>
    private partial Task LogChat_Implementation(string text)
    {
        Console.WriteLine($"[server] chat: {text}");
        return Task.CompletedTask;
    }

    /// <summary>그룹 다형성: 실제 타입(ShoutChatLine)이 보존돼 도착한다.</summary>
    private partial Task ChatMessage_Implementation(ChatLine line)
    {
        Console.WriteLine($"[server] {line.Describe()} ({line.GetType().Name})");
        return Task.CompletedTask;
    }

    /// <summary>제네릭 ①: T 슬롯이 허용 집합(int/string) 안에서만 컴파일·런타임 양쪽에 걸린다.</summary>
    private partial Task<T> GetConfig_Implementation<T>()
        => Task.FromResult<T>(typeof(T) == typeof(int) ? (T)(object)42 : (T)(object)"default");

    /// <summary>제네릭 ②: 호출 측 타입 추론으로 일반 호출처럼 쓴다.</summary>
    private partial Task<string> Describe_Implementation<T>(T value)
        => Task.FromResult($"{typeof(T).Name}={value}");

    private partial Task<T1> Blend_Implementation<T1, T2, T3>(T2 left, T3 right)
    {
        Console.WriteLine($"[server] Blend<{typeof(T1).Name}, {typeof(T2).Name}, {typeof(T3).Name}> left={left} right={right?.GetType().Name ?? "null"}");
        return Task.FromResult<T1>(default!);
    }

    /// <summary>제네릭 ④: [GenericMessage] 구성(ClassId)이 T 를 와이어에서 식별한다.</summary>
    private partial Task Unwrap_Implementation<T>(GiftBox<T> box)
    {
        string detail = box.Gift switch { ChatLine c => c.Text, Token t => $"token#{t.Value}", _ => "?" };
        Console.WriteLine($"[server] Unwrap<{typeof(T).Name}>: {detail}");
        return Task.CompletedTask;
    }

    /// <summary>F14 검증 게이트: 구현보다 먼저 호출된다. false 면 TransferGold_Implementation 은
    /// 실행되지 않고 허브가 RpcErrorCode.ValidationFailed(7) 오류 응답을 보낸다(클라 RpcFaultException 관찰).</summary>
    private partial Task<bool> TransferGold_Validate(int fromPlayer, int toPlayer, int amount)
    {
        bool pass = amount > 0 && fromPlayer != toPlayer;
        Console.WriteLine($"[server] TransferGold_Validate from={fromPlayer} to={toPlayer} amount={amount} -> {(pass ? "pass" : "reject")}");
        return Task.FromResult(pass);
    }

    private partial Task<int> TransferGold_Implementation(int fromPlayer, int toPlayer, int amount)
    {
        Console.WriteLine($"[server] TransferGold_Implementation {fromPlayer}->{toPlayer} x{amount}");
        return Task.FromResult(amount);
    }
}
