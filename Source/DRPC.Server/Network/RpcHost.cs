using System.Collections.Concurrent;
using Communication.Network.RUDP;
using Communication.Shared.Channels;
using DRPC.Shared.Network;

namespace DRPC.Server.Network;

/// <summary>
/// RUDP 리스너 수명. 생성된 <c>{Hub}.ListenAsync</c> 가 이 메서드를 부른다.
/// peer 마다 허브를 1개 만들고, 중지 시 리스너와 peer 허브를 함께 정리한다.
/// </summary>
public static class RpcHost
{
    /// <exception cref="InvalidOperationException">바인딩 실패·이미 시작된 리스너.</exception>
    public static Task<RpcListenHandle> ListenAsync<THub>(
        int port,
        string? connectionKey,
        Func<IMessageChannel, THub> hubFactory,
        Func<THub, Task>? onConnected = null,
        CancellationToken cancellationToken = default)
        where THub : Shared.Network.HubBase
    {
        var listener = new RudpListener(System.Net.IPAddress.Any, port);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var peers = new ConcurrentDictionary<THub, byte>();

        listener.Accepted += channel =>
        {
            THub hub = hubFactory(channel);
            peers.TryAdd(hub, 0);
            hub.Disconnected += () => peers.TryRemove(hub, out _);

            if (onConnected is null)
            {
                return;
            }

            _ = NotifyAsync(hub, onConnected);

            static async Task NotifyAsync(THub target, Func<THub, Task> callback)
            {
                try
                {
                    await callback(target).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // 접속 콜백 예외는 수신 경로를 죽이지 않는다(콘솔 의존 금지 — Trace 로만 남긴다).
                    System.Diagnostics.Trace.TraceError($"onConnected 예외: {e}");
                }
            }
        };

        void Stop()
        {
            listener.Stop();
            foreach (THub hub in peers.Keys.ToArray())
            {
                hub.Dispose();
            }

            peers.Clear();
            stopped.TrySetResult(true);
        }

        try
        {
            listener.Start(HubSessionFactory.CreateTransportOptions(connectionKey));
        }
        catch
        {
            Stop();
            linkedCts.Dispose();
            throw;
        }

        var handle = new RpcListenHandle(Stop, linkedCts) { ListenTask = stopped.Task };

        // 취소로도 중지가 관찰돼야 한다(ListenTask 가 영구 미완료로 남지 않도록).
        linkedCts.Token.Register(static state => ((Action)state!).Invoke(), new Action(Stop), useSynchronizationContext: false);

        return Task.FromResult(handle);
    }
}
