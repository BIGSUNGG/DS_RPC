using System.Collections.Concurrent;
using System.Reflection;
using Communication.Shared.Sessions;
using RPC.Shared.Interface;
using RPC.Shared.Message;
using System.Linq;

namespace RPC.Shared.Network;

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
    ISession _session;
    
    /// <summary>
    /// RPC 요청에 들어온 Method Id에 맞게 호출되어야 하는 Action 목록
    /// </summary>
    protected Dictionary<int/*method id*/, Func<byte[], byte[]>/*method matched to method id*/> MethodCallActions = new ();
    
    /// <summary>
    /// 사용 안 된 Call Id 중 제일 작은 수
    /// </summary>
    private uint _notUsedMinCallId = 0;
    /// <summary>
    /// 사용되어서 안 쓰이고 있는 Call Id 목록
    /// </summary>
    private ConcurrentStack<uint> _usedCallId = new();
    /// <summary>
    /// RPC 함수 호출 후 반환 값을 기다리는 태스크 소스
    /// </summary>
    protected ConcurrentDictionary<uint, TaskCompletionSource<byte[]>> WaitResponseTasks = new();
    
    public HubBase(Func<HubBase, ISession> sessionFactory)
    {
        _session = sessionFactory.Invoke(this);
    }

    protected async Task<byte[]> RequestRPC(int methodId, byte[] parameterData)
    {
        uint callId = _usedCallId.TryPop(out callId) ? callId : _notUsedMinCallId++;
        
        ProcedureCallRequestMessage requestMessage = new ProcedureCallRequestMessage(callId, methodId, parameterData);
        await _session.SendAsync(requestMessage);
        
        TaskCompletionSource<byte[]> waitResponseTask = new();
        WaitResponseTasks.TryAdd(callId, waitResponseTask);
        
        await waitResponseTask.Task;
        return waitResponseTask.Task.Result;
    }

    public void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message)
    {
        MethodCallActions.TryGetValue(message.MethodId, out Func<byte[], byte[]> methodCallAction);
        if(methodCallAction == null)
            throw new ArgumentException($"The method {message.MethodId} does not exist.");
        
        methodCallAction.Invoke(message.ParameterData);
    }
    
    public void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message)
    {
        if(WaitResponseTasks.TryRemove(message.CallId, out var waitResponseTask) == false)
            throw new InvalidOperationException($"The task {message.CallId} does not exist.");
        
        waitResponseTask.SetResult(message.ReturnData);
        _usedCallId.Push(message.CallId);
    }
}