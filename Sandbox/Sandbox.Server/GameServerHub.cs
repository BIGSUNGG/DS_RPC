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
}
