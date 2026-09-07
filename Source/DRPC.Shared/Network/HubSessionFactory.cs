using System.Buffers;
using Communication.Network.RUDP;
using Communication.Shared.Channels;
using Communication.Shared.Messages;
using Communication.Shared.Sessions;
using DRPC.Shared.Interface;
using MessageProtocol.Serialize;

namespace DRPC.Shared.Network;

/// <summary>
/// 허브·세션 조립 보일러플레이트를 모은 팩토리. 생성된 코드는 이 헬퍼만 호출한다.
/// </summary>
public static class HubSessionFactory
{
    /// <summary>MessageProtocol 기반 메시지 변환기(단일 인스턴스).</summary>
    public static IMessageConverter Converter { get; } = new MessageProtocolConverter();

    /// <summary>
    /// RUDP 채널 위에 RPC 세션을 만든다. 세션은 채널을 소유하므로 Dispose 시 채널까지 정리된다.
    /// </summary>
    public static ISession CreateRudpSession(IMessageChannel channel, IHubBase hub)
        => CreateRudpSession(channel, hub, null);

    /// <summary>
    /// 큐·디스패치 옵션 지정 버전 — 앱이 <see cref="Communication.Shared.Messages.MessageQueueOptions"/> 로
    /// <c>FrameTimeout</c>(슬로로리스 방어, 기본 30초 — 첫 바이트 도착 후 프레임 완성 마감),
    /// <c>MaxFrameLength</c>(기본 4MB) 등 세션 수준 정책을 통합 관리한다(형제 제안 P3).
    /// 커스텀 옵션 사용 시 커스텀 허브 팩토리(<c>channel => new GameHub(h => HubSessionFactory.CreateRudpSession(channel, h, opts))</c>)로 조합한다.
    /// </summary>
    public static ISession CreateRudpSession(IMessageChannel channel, IHubBase hub,
        Communication.Shared.Messages.MessageQueueOptions? queueOptions)
        => new RudpSession(channel, Converter, session => new DRPCMessageHandler(session, hub), queueOptions);

    /// <summary>
    /// 접속 옵션. <paramref name="connectionKey"/> 가 null/빈 문자열이면 전송 스택 기본 키를 쓴다.
    /// <paramref name="connectTimeoutMs"/> 가 양수면 침묵 호스트(블랙홀) 연결 실패를 그 시간 이내로 확정한다
    /// (Communication 2.0.1 <c>RudpTransportOptions.ConnectTimeout</c>). 0이면(기본) 전송 스택 기본값을 유지하고 음수는 거부한다.
    /// <paramref name="maxConnections"/> 가 양수면 동시 수락 연결 수 상한으로 걸고(상한 도달 시 접속 요청은 즉시 거부·수락 계속,
    /// Communication 2.0.1 <c>RudpTransportOptions.MaxConnections</c> — 서버 쪽에서만 의미), 0이면(기본) 무제한이다. 음수는 거부한다.
    /// </summary>
    public static RudpTransportOptions CreateTransportOptions(
        string? connectionKey,
        int connectTimeoutMs = 0,
        int maxConnections = 0,
        bool enableCrc32c = false)
    {
        if (connectTimeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(connectTimeoutMs));
        }

        if (maxConnections < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConnections));
        }

        var options = new RudpTransportOptions();
        if (!string.IsNullOrEmpty(connectionKey))
        {
            options.ConnectionKey = connectionKey;
        }

        if (connectTimeoutMs > 0)
        {
            options.ConnectTimeout = connectTimeoutMs;
        }

        if (maxConnections > 0)
        {
            options.MaxConnections = maxConnections;
        }

        if (enableCrc32c)
        {
            options.Crc32cEnabled = true;
        }

        return options;
    }

    sealed class MessageProtocolConverter : IMessageConverter
    {
        public void Serialize(object message, IBufferWriter<byte> writer)
        {
            var buffer = MessageBufferWriter.Create();
            try
            {
                MessageSerializer.SerializeToWriter(message, ref buffer);
                byte[] bytes = buffer.ToArray();
                bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
                writer.Advance(bytes.Length);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        public object Deserialize(ReadOnlySpan<byte> message) => MessageSerializer.Deserialize(message);
    }
}
