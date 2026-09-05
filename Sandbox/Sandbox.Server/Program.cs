using Sandbox.Contracts;
using Sandbox.Server;

const int Port = 9050;
const string ConnectionKey = "sandbox-key";

await using var handle = await GameServerHub.ListenAsync(Port, ConnectionKey, async hub =>
{
    Console.WriteLine("[server] client connected — 서버가 클라이언트로 역호출 시작");

    // 클라이언트 계약(IGameClientProcedures)의 outgoing 스텁: 그대로 await 하면 된다.
    float sum = await hub.EchoSumAsync(new List<float> { 1.5f, 2.25f, 4f });
    Console.WriteLine($"[server] EchoSum -> {sum}");

    int count = await hub.CountConfigAsync("arena", new[] { 1, 2, 3 });
    Console.WriteLine($"[server] CountConfig -> {count}");

    // OneWay 은 응답 없이 보낸다.
    await hub.NotifyScoreAsync(new ScoreBoard
    {
        Map = "arena",
        Lines = { new ScoreLine { PlayerId = 7, Score = 42 } },
    });
    Console.WriteLine("[server] NotifyScore(one-way) sent");
});

Console.WriteLine($"[server] RUDP 리스너 가동 중 (127.0.0.1:{Port}, key={ConnectionKey})");
Console.WriteLine("[server] 중지하려면 ENTER.");
Console.ReadLine();
