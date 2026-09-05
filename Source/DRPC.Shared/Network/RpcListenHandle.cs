namespace DRPC.Shared.Network;

/// <summary>
/// 서버 리스닝 수명 핸들. <see cref="Dispose"/> 시 리스너를-stop 하고 등록한 peer 허브를 정리한다.
/// </summary>
public sealed class RpcListenHandle : IAsyncDisposable, IDisposable
{
    readonly Action? _stop;
    readonly CancellationTokenSource? _linkedCts;
    int _disposed;

    public RpcListenHandle(Action? stop, CancellationTokenSource? linkedCts = null)
    {
        _stop = stop;
        _linkedCts = linkedCts;
    }

    /// <summary>
    /// 리스닝 루프에 대응하는 Task. Dispose 와 별개로 await 할 수 있고, 중지·취소 시 반드시 완료된다.
    /// </summary>
    public Task? ListenTask { get; init; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _linkedCts?.Cancel();
        }
        catch
        {
            // 취소 요구 실패는 정리를 막지 않는다.
        }

        try
        {
            _stop?.Invoke();
        }
        catch
        {
            // 정리 중 예외는 호출자에게 전파하지 않는다.
        }

        _linkedCts?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
