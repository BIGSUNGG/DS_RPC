namespace DRPC.Shared.Network;

/// <summary>
/// <c>ListenAsync</c>가 반환하는 리스너 수명 핸들. Dispose 시 리스너를 중지하고 취소를 요청한다.
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

    /// <summary>리스닝 루프 Task (선택). Dispose와 별개로 await할 수 있다.</summary>
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
            // ignore
        }

        try
        {
            _stop?.Invoke();
        }
        catch
        {
            // ignore
        }

        _linkedCts?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
