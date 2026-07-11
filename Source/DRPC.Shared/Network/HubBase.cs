using System.Collections.Concurrent;
using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using DRPC.Shared.Message;
using Communication.Network.RUDP.Shared.Messages;

namespace DRPC.Shared.Network;

/// <summary>
/// RPC 통신을 위한 허브
/// </summary>
/// <typeparam name="TSPD">서버에서 구현한 함수 선언을 가지는 객체</typeparam>
/// <typeparam name="TCPD">클라이언트에서 구현한 함수 선언을 가지는 객체</typeparam>
public abstract class HubBase<TSPD, TCPD> : HubBase
    where TSPD : IServerProcedureDeclarations
    where TCPD : IClientProcedureDeclarations
{
    public HubBase(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}

public abstract class HubBase : IHubBase, IDisposable
{
    readonly ISession _session;

    /// <summary>
    /// RPC 요청에 들어온 Method Id에 맞게 호출되어야 하는 Action 목록
    /// </summary>
    protected Dictionary<int, Func<byte[], Task<byte[]>>> MethodCallActions = new();

    /// <summary>
    /// One-way로 등록된 Method Id (응답을 보내지 않음)
    /// </summary>
    protected HashSet<int> OneWayMethodIds = new();

    /// <summary>
    /// Incoming MethodId → 응답 전송 시 사용할 ReliableType
    /// </summary>
    protected Dictionary<int, ReliableType> MethodReliableTypes = new();

    /// <summary>
    /// 다음 할당할 Call Id (재사용하지 않음)
    /// </summary>
    private int _nextCallId;

    sealed class PendingCall
    {
        public PendingCall(TaskCompletionSource<byte[]> tcs, long deadlineUtcTicks)
        {
            Tcs = tcs;
            DeadlineUtcTicks = deadlineUtcTicks;
        }

        public TaskCompletionSource<byte[]> Tcs { get; }
        public long DeadlineUtcTicks { get; }
    }

    /// <summary>
    /// RPC 함수 호출 후 반환 값을 기다리는 태스크 소스
    /// </summary>
    readonly ConcurrentDictionary<uint, PendingCall> _pendingCalls = new();

    Timer? _timeoutTimer;
    readonly object _timeoutTimerGate = new();

    SemaphoreSlim? _incomingGate;
    int _maxConcurrentIncoming;
    readonly object _incomingGateLock = new();

    /// <summary>
    /// Outgoing RPC 응답 대기 타임아웃. <see cref="Timeout.InfiniteTimeSpan"/>이면 무제한.
    /// </summary>
    public TimeSpan RpcTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 동시 Incoming 처리 상한. 0이면 무제한. 초과 시 non-one-way는 <see cref="RpcErrorCode.Overloaded"/>,
    /// one-way는 drop.
    /// </summary>
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

    /// <summary>
    /// 세션 끊김 또는 <see cref="Disconnect"/> 호출 시 발생.
    /// </summary>
    public event Action? Disconnected;

    bool _disposed;

    public HubBase(Func<HubBase, ISession> sessionFactory)
    {
        _session = sessionFactory.Invoke(this);
    }

    protected async Task SendRPC(int methodId, byte[] parameterData, ReliableType reliableType)
    {
        // OneWay: CallId 고정 0 — 응답/풀 재사용 없음
        const uint callId = 0;
        MessageSendContext messageSendContext = new MessageSendContext();
        messageSendContext.Reliable = reliableType;
        ProcedureCallRequestMessage requestMessage = new ProcedureCallRequestMessage(callId, methodId, parameterData);
        await _session.SendAsync(requestMessage, messageSendContext).ConfigureAwait(false);
    }

    protected async Task<byte[]> RequestRPC(int methodId, byte[] parameterData, ReliableType reliableType)
    {
        uint callId = AllocateCallId();
        TaskCompletionSource<byte[]> waitResponseTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
        long deadline = ComputeDeadlineUtcTicks();
        if (!_pendingCalls.TryAdd(callId, new PendingCall(waitResponseTask, deadline)))
        {
            throw new InvalidOperationException($"The call id {callId} is already in use.");
        }

        if (deadline > 0)
        {
            EnsureTimeoutTimer();
        }

        try
        {
            MessageSendContext messageSendContext = new MessageSendContext();
            messageSendContext.Reliable = reliableType;
            ProcedureCallRequestMessage requestMessage = new ProcedureCallRequestMessage(callId, methodId, parameterData);
            await _session.SendAsync(requestMessage, messageSendContext).ConfigureAwait(false);
            return await waitResponseTask.Task.ConfigureAwait(false);
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

    void EnsureTimeoutTimer()
    {
        lock (_timeoutTimerGate)
        {
            if (_timeoutTimer != null || _disposed)
            {
                return;
            }

            _timeoutTimer = new Timer(static state =>
            {
                var hub = (HubBase)state!;
                hub.ScanTimeouts();
            }, this, dueTime: 1000, period: 1000);
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
                pending.Tcs.TrySetException(
                    new TimeoutException($"RPC call {pair.Key} timed out after {RpcTimeout}."));
            }
        }
    }

    uint AllocateCallId()
    {
        // 0은 OneWay 예약 — RequestRPC는 1부터
        uint id;
        do
        {
            id = (uint)Interlocked.Increment(ref _nextCallId);
        } while (id == 0);

        return id;
    }

    public void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message)
    {
        _ = ProcessRequestAsync(message);
    }

    async Task ProcessRequestAsync(ProcedureCallRequestMessage message)
    {
        bool oneWay = OneWayMethodIds.Contains(message.MethodId);
        SemaphoreSlim? gate = null;
        lock (_incomingGateLock)
        {
            gate = _incomingGate;
        }

        if (gate != null)
        {
            if (!await gate.WaitAsync(0).ConfigureAwait(false))
            {
                if (!oneWay)
                {
                    await SendErrorAsync(message.CallId, RpcErrorCode.Overloaded,
                        "Server is at MaxConcurrentIncoming capacity.",
                        ResolveReliableType(message.MethodId)).ConfigureAwait(false);
                }

                return;
            }
        }

        try
        {
            if (!MethodCallActions.TryGetValue(message.MethodId, out Func<byte[], Task<byte[]>>? methodCallAction) ||
                methodCallAction == null)
            {
                if (!oneWay)
                {
                    await SendErrorAsync(message.CallId, RpcErrorCode.UnknownMethod,
                        $"The method {message.MethodId} does not exist.",
                        ResolveReliableType(message.MethodId)).ConfigureAwait(false);
                }

                return;
            }

            ReliableType reliable = ResolveReliableType(message.MethodId);
            byte[] result = await methodCallAction(message.ParameterData).ConfigureAwait(false);
            if (!oneWay)
            {
                MessageSendContext ctx = new MessageSendContext { Reliable = reliable };
                await _session.SendAsync(new ProcedureCallResponseMessage(message.CallId, result), ctx)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (!oneWay)
            {
                try
                {
                    await SendErrorAsync(message.CallId, RpcErrorCode.Unhandled, ex.Message,
                        ResolveReliableType(message.MethodId)).ConfigureAwait(false);
                }
                catch
                {
                    // ignore send failures while reporting errors
                }
            }
        }
        finally
        {
            gate?.Release();
        }
    }

    ReliableType ResolveReliableType(int methodId)
    {
        if (MethodReliableTypes.TryGetValue(methodId, out var reliable))
        {
            return reliable;
        }

        return ReliableType.ReliableOrdered;
    }

    Task SendErrorAsync(uint callId, int errorCode, string message, ReliableType reliableType)
    {
        MessageSendContext ctx = new MessageSendContext { Reliable = reliableType };
        return _session.SendAsync(new ProcedureCallErrorMessage(callId, errorCode, message), ctx);
    }

    public void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message)
    {
        if (!_pendingCalls.TryRemove(message.CallId, out var pending))
        {
            return;
        }

        pending.Tcs.TrySetResult(message.ReturnData);
    }

    public void OnReceiveRPCErrorMessage(ProcedureCallErrorMessage message)
    {
        if (!_pendingCalls.TryRemove(message.CallId, out var pending))
        {
            return;
        }

        pending.Tcs.TrySetException(new RpcFaultException(message.CallId, message.ErrorCode, message.Message));
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

    /// <summary>
    /// 대기 중 RPC를 취소하고 세션을 끊은 뒤 <see cref="Disconnected"/>를 발생시킨다.
    /// </summary>
    public void Disconnect()
    {
        CancelPendingCalls(new InvalidOperationException("RPC session disconnected."));
        try
        {
            _session.Disconnect();
        }
        catch
        {
            // ignore
        }

        RaiseDisconnected();
    }

    /// <summary>
    /// MessageHandler 등 외부에서 끊김을 통지할 때 사용. pending 취소 후 이벤트만 발생.
    /// </summary>
    public void NotifyDisconnected(Exception? reason = null)
    {
        CancelPendingCalls(reason ?? new InvalidOperationException("RPC session disconnected."));
        RaiseDisconnected();
    }

    void RaiseDisconnected()
    {
        try
        {
            Disconnected?.Invoke();
        }
        catch
        {
            // subscriber exceptions must not escape
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
