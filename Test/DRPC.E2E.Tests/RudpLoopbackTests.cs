using Communication.Shared.Sessions;
using DRPC;
using DRPC.Client.Network;
using DRPC.Shared;
using DRPC.Shared.Interface;
using DRPC.Shared.Message;
using DRPC.Shared.Network;
using Xunit;

namespace DRPC.E2E.Tests;

/// <summary>
/// 실제 RUDP(127.0.0.1) 왕복 검증. 전송 방식·OneWay·역호출·오류 경로를 와이어 위에서 확인한다.
/// 테스트마다 다른 포트를 써서 병렬 실행을 안전하게 한다.
/// </summary>
public class RudpLoopbackTests
{
    const string Key = "e2e-key";
    static int _portSeed = 9600;

    static int NextPort() => Interlocked.Add(ref _portSeed, 7);

    [Fact]
    public async Task Default_mode_is_reliable_ordered_and_roundtrips()
    {
        var (server, client, handle) = await PairAsync();
        await using var _ = handle;

        Assert.Equal(5, await Within(client.AddAsync(2, 3)));
        Assert.NotNull(server);
        client.Dispose();
    }

    [Fact]
    public async Task Sequenced_override_roundtrips()
    {
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;

        Assert.Equal("echo:udp", await Within(client.EchoAsync("udp")));
        client.Dispose();
    }

    [Fact]
    public async Task Unreliable_override_void_call_is_answered()
    {
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;

        // void + non-OneWay 이라 빈 응답까지 왕복한다. Unreliable 은 루프백에서 유실되지 않는다.
        await Within(client.PingAsync(11));
        client.Dispose();
    }

    [Fact]
    public async Task OneWay_call_reaches_peer_without_response()
    {
        string note = "note-" + Guid.NewGuid().ToString("N");
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;

        await Within(client.NoteAsync(note));
        await WaitUntilAsync(() => E2EServerHub.ReceivedNotes.Contains(note));
        client.Dispose();
    }

    [Fact]
    public async Task Message_arguments_and_results_roundtrip()
    {
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;

        OrderSummary summary = await Within(client.PlaceOrderAsync(new Order
        {
            Item = "cup",
            Quantity = 2,
            Tags = { 1, 2, 3 },
        }));

        Assert.Equal("cupx2", summary.Receipt);
        Assert.Equal(6m, summary.Total);
        client.Dispose();
    }

    [Fact]
    public async Task Server_can_call_back_into_client_contract()
    {
        int port = NextPort();
        var observed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var handle = await E2EServerHub.ListenAsync(port, Key, async hub =>
        {
            observed.TrySetResult(await hub.ClientValueAsync());
        });
        using var client = await E2EClientHub.ConnectAsync("127.0.0.1", port, Key);

        Assert.Equal(4242, await Within(observed.Task));
    }

    [Fact]
    public async Task Group_message_keeps_runtime_type_over_the_wire()
    {
        int port = NextPort();
        string text = "shout-" + Guid.NewGuid().ToString("N");

        await using var handle = await E2EServerHub.ListenAsync(port, Key, async hub =>
        {
            await hub.ReceiveLineAsync(new ShoutChatLine { Text = text });
        });
        using var client = await E2EClientHub.ConnectAsync("127.0.0.1", port, Key);

        await WaitUntilAsync(() => E2EClientHub.ReceivedLines.Any(line => line.EndsWith(":" + text, StringComparison.Ordinal)
            && line.StartsWith("ShoutChatLine:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Unknown_method_id_yields_unknown_method_fault()
    {
        int port = NextPort();
        await using var handle = await E2EServerHub.ListenAsync(port, Key, _ => Task.CompletedTask);

        // 서버가 모르는 MethodId 를 직접 심는다(생성 스텁을 우회하는 원시 요청).
        using var rogue = await RpcClient.ConnectAsync("127.0.0.1", port, Key,
            channel => new RogueClientHub(hub => HubSessionFactory.CreateRudpSession(channel, hub)));

        RpcFaultException fault = await Assert.ThrowsAsync<RpcFaultException>(() => Within(rogue.ProbeAsync(4242)));
        Assert.Equal(RpcErrorCode.UnknownMethod, fault.ErrorCode);
    }

    [Fact]
    public async Task Implementation_exception_surfaces_as_unhandled_fault()
    {
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;

        RpcFaultException fault = await Assert.ThrowsAsync<RpcFaultException>(() => Within(client.AlwaysFailsAsync()));
        Assert.Equal(RpcErrorCode.Unhandled, fault.ErrorCode);
        Assert.Contains("intentional failure", fault.Message);
        client.Dispose();
    }

    [Fact]
    public async Task Slow_response_triggers_client_timeout()
    {
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;
        client.RpcTimeout = TimeSpan.FromMilliseconds(200);

        await Assert.ThrowsAsync<TimeoutException>(() => Within(client.SlowAsync(3000), 8000));
        client.Dispose();
    }

    [Fact]
    public async Task ListenTask_completes_and_peer_notices_when_handle_is_disposed()
    {
        int port = NextPort();
        RpcListenHandle handle = await E2EServerHub.ListenAsync(port, Key, _ => Task.CompletedTask);
        var client = await E2EClientHub.ConnectAsync("127.0.0.1", port, Key);
        var dropped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Disconnected += () => dropped.TrySetResult();

        handle.Dispose();

        await Within(handle.ListenTask!, 5000);
        await Within(dropped.Task);
        client.Dispose();
    }

    [Fact]
    public async Task Concurrent_calls_keep_their_own_results()
    {
        var (_, client, handle) = await PairAsync();
        await using var _ = handle;

        Task<int>[] calls = Enumerable.Range(1, 20).Select(i => client.AddAsync(i, i)).ToArray();
        int[] results = await Within(Task.WhenAll(calls));

        Assert.Equal(Enumerable.Range(1, 20).Select(i => i * 2), results);
        client.Dispose();
    }

    static async Task<(E2EServerHub Server, E2EClientHub Client, RpcListenHandle Handle)> PairAsync()
    {
        int port = NextPort();
        var accepted = new TaskCompletionSource<E2EServerHub>(TaskCreationOptions.RunContinuationsAsynchronously);

        RpcListenHandle handle = await E2EServerHub.ListenAsync(port, Key, hub =>
        {
            accepted.TrySetResult(hub);
            return Task.CompletedTask;
        });

        E2EClientHub client = await E2EClientHub.ConnectAsync("127.0.0.1", port, Key);
        E2EServerHub server = await accepted.Task;
        return (server, client, handle);
    }

    static async Task<T> Within<T>(Task<T> task, int timeoutMs = 8000)
    {
        if (await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false) != task)
        {
            throw new TimeoutException($"주어진 시간({timeoutMs}ms) 내에 완료되지 않았습니다.");
        }

        return await task.ConfigureAwait(false);
    }

    static async Task Within(Task task, int timeoutMs = 8000)
    {
        if (await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false) != task)
        {
            throw new TimeoutException($"주어진 시간({timeoutMs}ms) 내에 완료되지 않았습니다.");
        }

        await task.ConfigureAwait(false);
    }

    static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 8000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25).ConfigureAwait(false);
        }

        throw new TimeoutException("주어진 시간 내에 조건이 충족되지 않았습니다.");
    }
}

/// <summary>
/// 생성 스텁 없이 원시 요청을 보내는 클라이언트(미등록 MethodId 경로를 검증하기 위한 용도).
/// 계약 자체를 비워 두어 스텁 생성을 없애고, 허브 자체는 partial 이어야 DRPCGEN001 이 나지 않는다.
/// </summary>
public interface IRogueServerProcedures : IServerProcedureDeclarations
{
}

public interface IRogueClientProcedures : IClientProcedureDeclarations
{
}

public partial class RogueClientHub : ClientHub<IRogueServerProcedures, IRogueClientProcedures>
{
    public RogueClientHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }

    public Task<byte[]> ProbeAsync(int methodId)
        => RequestRPC(methodId, Array.Empty<byte>(), RpcDeliveryMode.ReliableOrdered);
}
