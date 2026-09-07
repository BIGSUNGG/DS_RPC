using System.Buffers;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Communication.Shared.Channels;
using Communication.Shared.Connection;
using Communication.Shared.Sessions;
using DRPC.Shared.Message;
using DRPC.Shared.Network;

namespace DRPC.Benchmarks;

/// <summary>
/// HubBase 핫패스 벤치마크 — 루프백 메모리 세션으로 런타임(직렬화 제외 디스패치·대기 완성)만 측정한다.
/// 기준선: Document/03-Reference/Performance.md.
/// </summary>
[MemoryDiagnoser]
public class HubBenchmarks
{
    readonly byte[] _payload = { 1, 2, 3 };
    readonly byte[] _response = { 1, 2, 3 };
    BenchSession _session = null!;
    BenchHub _hub = null!;
    Action<object> _loopback = null!;

    [GlobalSetup]
    public void Setup()
    {
        _session = new BenchSession();
        _hub = new BenchHub(_ => _session);

        // 왕복 루프백: 송신된 요청을 즉시 응답으로 되돌린다(전송 스택 비용 제외).
        _loopback = m =>
        {
            if (m is ProcedureCallRequestMessage { CallId: not 0 } request)
            {
                _hub.OnReceiveRPCResponseMessage(new ProcedureCallResponseMessage(request.CallId, _response));
            }
        };
        _session.Route = _loopback;
        _hub.RegisterAction(1, _ => Task.FromResult(_response));
    }

    [Benchmark(Description = "Outgoing roundtrip (loopback session)")]
    public async Task<byte[]> OutgoingRoundtrip()
        => await _hub.Roundtrip(1, _payload);

    [Benchmark(Description = "Incoming dispatch (request→impl→response)")]
    public async Task IncomingDispatch()
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _session.Route = m =>
        {
            if (m is ProcedureCallResponseMessage)
            {
                done.TrySetResult();
            }
        };

        _hub.OnReceiveRPCRequestMessage(new ProcedureCallRequestMessage(1u, 1, _payload));
        await done.Task;

        _session.Route = _loopback; // 복원
    }

    [Benchmark(Description = "One-way send (CallId 0)")]
    public Task OneWaySend()
        => _hub.OneWay(2, _payload);

    [Benchmark(Description = "Converter serialize (RPC request)")]
    public long ConverterSerialize()
    {
        var writer = new ArrayBufferWriter<byte>();
        HubSessionFactory.Converter.Serialize(new ProcedureCallRequestMessage(1u, 1, _payload), writer);
        return writer.WrittenCount;
    }
}

/// <summary>HubBase protected 표면 노출(벤치 전용).</summary>
sealed class BenchHub : HubBase
{
    public BenchHub(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }

    /// <summary>왕복 호출 진입점 — 루프백 세션이 응답을 즉시 완성한다.</summary>
    public Task<byte[]> Roundtrip(int methodId, byte[] payload)
        => RequestRPC(methodId, payload, RpcDeliveryMode.ReliableOrdered);

    public Task OneWay(int methodId, byte[] payload)
        => SendRPC(methodId, payload, RpcDeliveryMode.ReliableOrdered);

    public void RegisterAction(int methodId, Func<byte[], Task<byte[]>> action)
        => MethodCallActions[methodId] = action;
}

/// <summary>송신을 라우팅 콜백으로만 전달하는 메모리 세션(전송 스택 비용 0).</summary>
sealed class BenchSession : ISession
{
    public Action<object>? Route { get; set; }

    public Task SendAsync(object message) => SendAsync(message, null);

    public Task SendAsync(object message, SendOptions? options)
    {
        Route?.Invoke(message);
        return Task.CompletedTask;
    }

    public Task SendAndFlushAsync(object message, SendOptions? options = null,
        System.Threading.CancellationToken cancellationToken = default)
        => SendAsync(message, options);

    public void Disconnect() { }

    public bool IsConnected() => true;

    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    public void Dispose() { }
}
