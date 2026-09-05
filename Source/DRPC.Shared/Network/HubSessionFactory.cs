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
        => new RudpSession(channel, Converter, session => new DRPCMessageHandler(session, hub));

    /// <summary>
    /// 접속 옵션. <paramref name="connectionKey"/> 가 null/빈 문자열이면 전송 스택 기본 키를 쓴다.
    /// </summary>
    public static RudpTransportOptions CreateTransportOptions(string? connectionKey)
    {
        var options = new RudpTransportOptions();
        if (!string.IsNullOrEmpty(connectionKey))
        {
            options.ConnectionKey = connectionKey;
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
