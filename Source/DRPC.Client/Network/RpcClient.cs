using Communication.Network.RUDP;
using Communication.Shared.Channels;
using DRPC.Shared.Network;

namespace DRPC.Client.Network;

/// <summary>
/// RUDP 접속 후 허브를 조립한다. 생성된 <c>{Hub}.ConnectAsync</c> 가 이 메서드를 부른다.
/// </summary>
public static class RpcClient
{
    /// <exception cref="InvalidOperationException">접속 거부·호스트 해석 실패·재시도 소진.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 취소.</exception>
    public static Task<THub> ConnectAsync<THub>(
        string host,
        int port,
        string? connectionKey,
        Func<IMessageChannel, THub> hubFactory,
        CancellationToken cancellationToken = default)
        where THub : Shared.Network.HubBase
        => ConnectAsync(host, port, connectionKey, 0, hubFactory, cancellationToken);

    /// <summary>
    /// <paramref name="connectTimeoutMs"/> 를 지정하면 침묵 호스트(패킷 블랙홀)에 대한 연결 실패를
    /// 그 시간 이내로 확정한다. 0 이하면 전송 스택 기본값(약 5초)을 유지한다.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="connectTimeoutMs"/> 가 음수.</exception>
    /// <exception cref="InvalidOperationException">접속 거부·호스트 해석 실패·재시도 소진.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 취소.</exception>
    public static async Task<THub> ConnectAsync<THub>(
        string host,
        int port,
        string? connectionKey,
        int connectTimeoutMs,
        Func<IMessageChannel, THub> hubFactory,
        CancellationToken cancellationToken = default)
        where THub : Shared.Network.HubBase
    {
        if (connectTimeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(connectTimeoutMs));
        }

        return await ConnectCoreAsync<THub>(host, port,
            HubSessionFactory.CreateTransportOptions(connectionKey, connectTimeoutMs),
            hubFactory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="RpcEndpointOptions"/> 로 전송 옵션(키·연결 타임아웃·CRC32c 무결성 등)을 일괄 지정한다.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="endpointOptions"/> 가 null.</exception>
    /// <exception cref="InvalidOperationException">접속 거부·호스트 해석 실패·재시도 소진.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 취소.</exception>
    public static Task<THub> ConnectWithOptionsAsync<THub>(
        string host,
        int port,
        RpcEndpointOptions endpointOptions,
        Func<IMessageChannel, THub> hubFactory,
        CancellationToken cancellationToken = default)
        where THub : Shared.Network.HubBase
    {
        if (endpointOptions is null)
        {
            throw new ArgumentNullException(nameof(endpointOptions));
        }

        return ConnectCoreAsync<THub>(host, port, endpointOptions.ToTransportOptions(),
            hubFactory, cancellationToken);
    }

    /// <summary>전송 옵션 조립 후 접속·허브 팩토리 조립까지의 단일 경로(중복 제거 — 구조 검토 1건).</summary>
    static async Task<THub> ConnectCoreAsync<THub>(
        string host,
        int port,
        RudpTransportOptions transportOptions,
        Func<IMessageChannel, THub> hubFactory,
        CancellationToken cancellationToken)
        where THub : Shared.Network.HubBase
    {
        var connector = new RudpConnector();

        if (!await connector.ConnectAsync(host, port, transportOptions, cancellationToken).ConfigureAwait(false)
            || connector.Channel is null)
        {
            throw new InvalidOperationException("Failed to connect to server.");
        }

        IMessageChannel channel = connector.Channel;
        return hubFactory(channel);
    }
}
