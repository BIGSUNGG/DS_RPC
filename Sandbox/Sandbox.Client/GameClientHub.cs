using DRPC.Client.Network;
using Sandbox.Contracts;

namespace Sandbox.Client;

/// <summary>
/// 클라이언트 측 허브. 서버 계약(<see cref="IGameServerProcedures"/>)은 <c>*Async</c> 로 호출하고,
/// 클라이언트 계약(<see cref="IGameClientProcedures"/>)은 <c>*_Implementation</c> partial 로 구현한다.
/// </summary>
public partial class GameClientHub : ClientHub<IGameServerProcedures, IGameClientProcedures>
{
    private partial Task<float> EchoSum_Implementation(List<float> values)
    {
        Console.WriteLine($"[client] EchoSum called with {values.Count} values");
        return Task.FromResult(values.Sum());
    }

    private partial Task<int> CountConfig_Implementation(string? label, int[] values)
    {
        Console.WriteLine($"[client] CountConfig called: label={(label ?? "<null>")}, values={values.Length}");
        return Task.FromResult(values.Length);
    }

    private partial Task NotifyScore_Implementation(ScoreBoard score)
    {
        Console.WriteLine($"[client] NotifyScore(one-way): map={score.Map}, lines={score.Lines.Count}, first={score.Lines[0].Score}");
        return Task.CompletedTask;
    }
}
