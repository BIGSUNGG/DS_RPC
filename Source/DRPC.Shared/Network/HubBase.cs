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
/// <typeparam name="TCD">클라이언트에서 구현한 함수 선언을 가지는 객체</typeparam>
public abstract class HubBase<TSPD, TCPD> : HubBase
    where TSPD : IServerProcedureDeclarations
    where TCPD : IClientProcedureDeclarations
{
    public HubBase(Func<HubBase, ISession> sessionFactory)
        : base(sessionFactory)
    {
    }
}

public abstract class HubBase : IHubBase
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
    /// 사용 안 된 Call Id 중 제일 작은 수 (다음 할당 후보)
    /// </summary>
    private int _nextCallId;

    /// <summary>
    /// 사용되어서 안 쓰이고 있는 Call Id 목록
    /// </summary>
    private readonly ConcurrentStack<uint> _usedCallId = new();

    /// <summary>
    /// RPC 함수 호출 후 반환 값을 기다리는 태스크 소스
    /// </summary>
    protected ConcurrentDictionary<uint, TaskCompletionSource<byte[]>> WaitResponseTasks = new();

    /// <summary>
    /// Outgoing RPC 응답 대기 타임아웃. <see cref="Timeout.InfiniteTimeSpan"/>이면 무제한.
    /// </summary>
    public TimeSpan RpcTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public HubBase(Func<HubBase, ISession> sessionFactory)
    {
        _session = sessionFactory.Invoke(this);
    }

    protected async Task SendRPC(int methodId, byte[] parameterData, ReliableType reliableType)
    {
        uint callId = AllocateCallId();
        MessageSendContext messageSendContext = new MessageSendContext();
        messageSendContext.Reliable = reliableType;
        ProcedureCallRequestMessage requestMessage = new ProcedureCallRequestMessage(callId, methodId, parameterData);
        await _session.SendAsync(requestMessage, messageSendContext).ConfigureAwait(false);
    }

    protected async Task<byte[]> RequestRPC(int methodId, byte[] parameterData, ReliableType reliableType)
    {
        uint callId = AllocateCallId();
        TaskCompletionSource<byte[]> waitResponseTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!WaitResponseTasks.TryAdd(callId, waitResponseTask))
        {
            _usedCallId.Push(callId);
            throw new InvalidOperationException($"The call id {callId} is already in use.");
        }

        CancellationTokenSource? timeoutCts = CreateTimeoutCts();
        CancellationTokenRegistration timeoutRegistration = default;
        if (timeoutCts != null)
        {
            timeoutRegistration = timeoutCts.Token.Register(() =>
            {
                if (WaitResponseTasks.TryRemove(callId, out var tcs))
                {
                    tcs.TrySetException(new TimeoutException($"RPC call {callId} timed out after {RpcTimeout}."));
                    _usedCallId.Push(callId);
                }
            });
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
            timeoutRegistration.Dispose();
            timeoutCts?.Dispose();
        }
    }

    CancellationTokenSource? CreateTimeoutCts()
    {
        if (RpcTimeout == Timeout.InfiniteTimeSpan || RpcTimeout <= TimeSpan.Zero)
        {
            return null;
        }

        var cts = new CancellationTokenSource();
        cts.CancelAfter(RpcTimeout);
        return cts;
    }

    uint AllocateCallId()
    {
        if (_usedCallId.TryPop(out uint reused))
        {
            return reused;
        }

        return (uint)Interlocked.Increment(ref _nextCallId) - 1;
    }

    public void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message)
    {
        _ = ProcessRequestAsync(message);
    }

    async Task ProcessRequestAsync(ProcedureCallRequestMessage message)
    {
        bool oneWay = OneWayMethodIds.Contains(message.MethodId);

        try
        {
            if (!MethodCallActions.TryGetValue(message.MethodId, out Func<byte[], Task<byte[]>>? methodCallAction) ||
                methodCallAction == null)
            {
                if (!oneWay)
                {
                    await SendErrorAsync(message.CallId, RpcErrorCode.UnknownMethod,
                        $"The method {message.MethodId} does not exist.").ConfigureAwait(false);
                }

                return;
            }

            byte[] result = await methodCallAction(message.ParameterData).ConfigureAwait(false);
            if (!oneWay)
            {
                await _session.SendAsync(new ProcedureCallResponseMessage(message.CallId, result)).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (!oneWay)
            {
                try
                {
                    await SendErrorAsync(message.CallId, RpcErrorCode.Unhandled, ex.Message).ConfigureAwait(false);
                }
                catch
                {
                    // ignore send failures while reporting errors
                }
            }
        }
    }

    Task SendErrorAsync(uint callId, int errorCode, string message)
    {
        return _session.SendAsync(new ProcedureCallErrorMessage(callId, errorCode, message));
    }

    public void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message)
    {
        if (!WaitResponseTasks.TryRemove(message.CallId, out var waitResponseTask))
        {
            return;
        }

        waitResponseTask.TrySetResult(message.ReturnData);
        _usedCallId.Push(message.CallId);
    }

    public void OnReceiveRPCErrorMessage(ProcedureCallErrorMessage message)
    {
        if (!WaitResponseTasks.TryRemove(message.CallId, out var waitResponseTask))
        {
            return;
        }

        waitResponseTask.TrySetException(new RpcFaultException(message.CallId, message.ErrorCode, message.Message));
        _usedCallId.Push(message.CallId);
    }

    public void CancelPendingCalls(Exception reason)
    {
        foreach (var pair in WaitResponseTasks)
        {
            if (WaitResponseTasks.TryRemove(pair.Key, out var tcs))
            {
                tcs.TrySetException(reason);
                _usedCallId.Push(pair.Key);
            }
        }
    }
}
