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
    public static async Task<THub> ConnectAsync<THub>(
        string host,
        int port,
        string? connectionKey,
        Func<IMessageChannel, THub> hubFactory,
        CancellationToken cancellationToken = default)
        where THub : Shared.Network.HubBase
    {
        var connector = new RudpConnector();

        if (!await connector.ConnectAsync(host, port, HubSessionFactory.CreateTransportOptions(connectionKey),
                cancellationToken).ConfigureAwait(false) || connector.Channel is null)
        {
            throw new InvalidOperationException("Failed to connect to server.");
        }

        IMessageChannel channel = connector.Channel;
        return hubFactory(channel);
    }
}
