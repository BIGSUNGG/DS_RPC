using System.Collections.Concurrent;
using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Message;

namespace DRPC.Shared.Network;

/// <summary>
/// RPC 허브의 공용 런타임. <typeparamref name="TSPD"/>(서버 계약)와 <typeparamref name="TCPD"/>(클라이언트 계약)를
/// 매개변수로 받아 어느 쪽 엔드포인트든 같은 런타임을 쓴다.
/// </summary>
/// <typeparam name="TSPD">서버가 구현하는 함수 선언 인터페이스.</typeparam>
/// <typeparam name="TCPD">클라이언트가 구현하는 함수 선언 인터페이스.</typeparam>
public abstract class HubBase<TSPD, TCPD> : HubBase
    where TSPD : IServerProcedureDeclarations
    where TCPD : IClientProcedureDeclarations
{
    protected HubBase(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}

/// <summary>
/// 왕복 RPC 런타임: outgoing 호출(CallId 할당·응답 대기·타임아웃), incoming 디스패치(동시 상한), 끊김 정리.
/// 생성된 허브 스텁만 이 클래스의 protected API 를 사용한다 — 사용자 코드는 직접 다루지 않는다.
/// </summary>
public abstract class HubBase : IHubBase, IDisposable
{
    readonly ISession _session;

    /// <summary>MethodId → 처리 위임(페이로드 바이트 → 응답 페이로드 바이트).</summary>
    protected Dictionary<int, Func<byte[], Task<byte[]>>> MethodCallActions { get; } = new();

    /// <summary>Incoming MethodId → 응답·오류 전송 시 사용할 전송 방식(요청의 방식과 일치시킨다).</summary>
    protected Dictionary<int, RpcDeliveryMode> MethodDeliveryModes { get; } = new();

    /// <summary>다음 CallId. 0 은 one-way 예약이라 절대 할당하지 않는다.</summary>
    int _nextCallId;

    sealed class PendingCall
    {
        public PendingCall(TaskCompletionSource<byte[]> tcs, long deadlineUtcTicks)
        {
            Tcs = tcs;
            DeadlineUtcTicks = deadlineUtcTicks;
        }

        public TaskCompletionSource<byte[]> Tcs { get; }

        /// <summary>0 이면 무제한(스캔 대상 아님).</summary>
        public long DeadlineUtcTicks { get; }
    }

    readonly ConcurrentDictionary<uint, PendingCall> _pendingCalls = new();

    Timer? _timeoutTimer;
    readonly object _timeoutTimerGate = new();

    SemaphoreSlim? _incomingGate;
    int _maxConcurrentIncoming;
    readonly object _incomingGateLock = new();

    int _disconnectRaised;
    bool _disposed;

    /// <summary>
    /// outgoing RPC 응답 대기 상한. 기본 30초. <see cref="Timeout.InfiniteTimeSpan"/> 또는 0 이하이면 무제한.
    /// 만료된 호출은 <see cref="TimeoutException"/> 으로 완료된다.
    /// </summary>
    public TimeSpan RpcTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 동시 Incoming 처리 상한. 0(기본)이면 무제한. 초과하면 non-one-way 요청은
    /// <see cref="RpcErrorCode.Overloaded"/> 오류를 받고 one-way 은 버려진다.
    /// </summary>
    /// <remarks>연결 직후·유휴 상태에서 설정한다. 처리 중인 실행과의 세마포어 재교대는 지원하지 않는다.</remarks>
    public int MaxConcurrentIncoming
    {
        get => _maxConcurrentIncoming;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            lock (_incomingGateLock)
            {
                _maxConcurrentIncoming = value;
                _incomingGate?.Dispose();
                _incomingGate = value > 0 ? new SemaphoreSlim(value, value) : null;
            }
        }
    }

    /// <summary>연결이 끊겼을 때 발생(세션당 1회). 대기 중 호출은 이미 실패 처리된 뒤다.</summary>
    public event Action? Disconnected;

    protected HubBase(Func<HubBase, ISession> sessionFactory)
    {
        if (sessionFactory is null)
        {
            throw new ArgumentNullException(nameof(sessionFactory));
        }

        _session = sessionFactory.Invoke(this);
    }

    /// <summary>세션 끊김 관측 등을 위한 전송 진입점. 생성된 코드가 사용하는 protected 표면.</summary>
    protected internal ISession Session => _session;

    /// <summary>one-way 요청을 보낸다. CallId 는 0 고정(응답 대기표 없음).</summary>
    protected async Task SendRPC(int methodId, byte[] parameterData, RpcDeliveryMode mode)
    {
        const uint callId = 0;
        var request = new ProcedureCallRequestMessage(callId, methodId, parameterData);
        await _session.SendAsync(request, mode.ToSendOptions()).ConfigureAwait(false);
    }

    /// <summary>
    /// 요청을 보내고 응답·오류·타임아웃·끊김 중 하나로 완료되는 응답 바이트를 기다린다.
    /// </summary>
    protected async Task<byte[]> RequestRPC(int methodId, byte[] parameterData, RpcDeliveryMode mode)
    {
        uint callId = AllocateCallId();
        var waitResponse = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        long deadline = ComputeDeadlineUtcTicks();

        if (!_pendingCalls.TryAdd(callId, new PendingCall(waitResponse, deadline)))
        {
            throw new InvalidOperationException($"The call id {callId} is already in use.");
        }

        if (deadline > 0)
        {
            EnsureTimeoutTimer();
        }

        try
        {
            var request = new ProcedureCallRequestMessage(callId, methodId, parameterData);
            await _session.SendAsync(request, mode.ToSendOptions()).ConfigureAwait(false);
            return await waitResponse.Task.ConfigureAwait(false);
        }
        finally
        {
            _pendingCalls.TryRemove(callId, out _);
        }
    }

    long ComputeDeadlineUtcTicks()
    {
        if (RpcTimeout == Timeout.InfiniteTimeSpan || RpcTimeout <= TimeSpan.Zero)
        {
            return 0;
        }

        return DateTime.UtcNow.Add(RpcTimeout).Ticks;
    }

    /// <summary>호버 공용 타이머 1개(1초 스캔). 호출당 CTS 는 만들지 않는다.</summary>
    void EnsureTimeoutTimer()
    {
        lock (_timeoutTimerGate)
        {
            if (_timeoutTimer != null || _disposed)
            {
                return;
            }

            _timeoutTimer = new Timer(static state => ((HubBase)state!).ScanTimeouts(), this,
                dueTime: 1000, period: 1000);
        }
    }

    void ScanTimeouts()
    {
        long now = DateTime.UtcNow.Ticks;
        foreach (var pair in _pendingCalls)
        {
            long deadline = pair.Value.DeadlineUtcTicks;
            if (deadline <= 0 || deadline > now)
            {
                continue;
            }

            if (_pendingCalls.TryRemove(pair.Key, out var pending))
            {
                pending.Tcs.TrySetException(new TimeoutException($"RPC call {pair.Key} timed out after {RpcTimeout}."));
            }
        }
    }

    uint AllocateCallId()
    {
        uint id;
        do
        {
            id = (uint)Interlocked.Increment(ref _nextCallId);
        }
        while (id == 0);

        return id;
    }

    /// <summary>수신 요청을 처리 큐로 넘긴다(전송 콜백 스레드를 점유하지 않는다).</summary>
    public void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message)
    {
        _ = ProcessRequestAsync(message);
    }

    async Task ProcessRequestAsync(ProcedureCallRequestMessage message)
    {
        bool oneWay = IsOneWay(message);

        SemaphoreSlim? gate;
        lock (_incomingGateLock)
        {
            gate = _incomingGate;
        }

        if (gate != null && !await gate.WaitAsync(0).ConfigureAwait(false))
        {
            if (!oneWay)
            {
                await SendErrorAsync(message.CallId, RpcErrorCode.Overloaded,
                    "Hub is at MaxConcurrentIncoming capacity.", ResolveMode(message.MethodId)).ConfigureAwait(false);
            }

            return;
        }

        try
        {
            if (!MethodCallActions.TryGetValue(message.MethodId, out Func<byte[], Task<byte[]>>? action) || action is null)
            {
                if (!oneWay)
                {
                    await SendErrorAsync(message.CallId, RpcErrorCode.UnknownMethod,
                        $"The method {message.MethodId} does not exist.", ResolveMode(message.MethodId)).ConfigureAwait(false);
                }

                return;
            }

            RpcDeliveryMode mode = ResolveMode(message.MethodId);
            byte[] result = await action(message.ParameterData).ConfigureAwait(false);

            if (!oneWay)
            {
                await _session.SendAsync(new ProcedureCallResponseMessage(message.CallId, result), mode.ToSendOptions())
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (oneWay)
            {
                return;
            }

            try
            {
                await SendErrorAsync(message.CallId, RpcErrorCode.Unhandled, ex.Message, ResolveMode(message.MethodId))
                    .ConfigureAwait(false);
            }
            catch
            {
                // 오류 보고 실패는 원래 실패를 가리지 않는다.
            }
        }
        finally
        {
            gate?.Release();
        }
    }

    /// <summary>
    /// 와이어상의 one-way 신호는 CallId 0 고정이다. <see cref="SendRPC"/> 는 0 을 보내고
    /// <see cref="RequestRPC"/> 는 1 부터 할당하므로 두 값은 겹치지 않는다.
    /// 수신 측 등록표로 판정하지 않는다 — 서버·클라이언트 계약이 같은 MethodId 를 서로 다르게 one-way 로
    /// 선언해도 어긋나지 않고, 미등록 MethodId 도 올바르게 취급된다.
    /// </summary>
    static bool IsOneWay(ProcedureCallRequestMessage message) => message.CallId == 0;

    /// <summary>요청 MethodId 에 등록된 전송 방식. 미등록이면 ReliableOrdered.</summary>
    RpcDeliveryMode ResolveMode(int methodId)
        => MethodDeliveryModes.TryGetValue(methodId, out var mode) ? mode : RpcDeliveryMode.ReliableOrdered;

    Task SendErrorAsync(uint callId, int errorCode, string message, RpcDeliveryMode mode)
        => _session.SendAsync(new ProcedureCallErrorMessage(callId, errorCode, message), mode.ToSendOptions());

    /// <summary>응답 바이트를 대기 중인 호출에 전달. 대기표가 없는 응답(지연 도착·중복)은 버린다.</summary>
    public void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message)
    {
        if (_pendingCalls.TryRemove(message.CallId, out var pending))
        {
            pending.Tcs.TrySetResult(message.ReturnData);
        }
    }

    /// <summary>오류 응답을 <see cref="RpcFaultException"/> 으로 대기 중인 호출에 전달한다.</summary>
    public void OnReceiveRPCErrorMessage(ProcedureCallErrorMessage message)
    {
        if (_pendingCalls.TryRemove(message.CallId, out var pending))
        {
            pending.Tcs.TrySetException(new RpcFaultException(message.CallId, message.ErrorCode, message.Message));
        }
    }

    public void CancelPendingCalls(Exception reason)
    {
        foreach (var pair in _pendingCalls)
        {
            if (_pendingCalls.TryRemove(pair.Key, out var pending))
            {
                pending.Tcs.TrySetException(reason);
            }
        }
    }

    /// <summary>대기 중 RPC를 실패시키고 세션을 끊은 뒤 <see cref="Disconnected"/> 를 발생시킨다.</summary>
    public void Disconnect()
    {
        CancelPendingCalls(new InvalidOperationException("RPC session disconnected."));

        try
        {
            _session.Disconnect();
        }
        catch
        {
            // 끊김 정리 중 예외는 호출자에게 전파하지 않는다.
        }

        RaiseDisconnected();
    }

    /// <summary>수신 경로(세션 이벤트)에서 끊김을 통지할 때 사용한다.</summary>
    public void NotifyDisconnected(Exception? reason)
    {
        CancelPendingCalls(reason ?? new InvalidOperationException("RPC session disconnected."));
        RaiseDisconnected();
    }

    void RaiseDisconnected()
    {
        if (Interlocked.Exchange(ref _disconnectRaised, 1) != 0)
        {
            return;
        }

        try
        {
            Disconnected?.Invoke();
        }
        catch
        {
            // 구독자 예외는 런타임을 죽이지 않는다.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        lock (_timeoutTimerGate)
        {
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;
        }

        Disconnect();

        lock (_incomingGateLock)
        {
            _incomingGate?.Dispose();
            _incomingGate = null;
        }
    }
}
