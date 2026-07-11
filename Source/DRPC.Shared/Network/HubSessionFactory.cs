using Communication.Shared.Messages;

namespace DRPC.Shared.Network;

/// <summary>
/// 생성 Connect/Listen 보일러플레이트를 줄이기 위한 메시지 변환기.
/// </summary>
public static class HubSessionFactory
{
    public static IMessageConverter CreateDefaultConverter() => DefaultConverter.Instance;

    sealed class DefaultConverter : IMessageConverter
    {
        public static readonly DefaultConverter Instance = new();

        public byte[] Serialize(object message)
        {
            return MessageProtocol.Serialize.MessageSerializer.Serialize(message);
        }

        public object Deserialize(global::System.ReadOnlySpan<byte> messageData)
        {
            return MessageProtocol.Serialize.MessageSerializer.Deserialize(messageData);
        }
    }
}
