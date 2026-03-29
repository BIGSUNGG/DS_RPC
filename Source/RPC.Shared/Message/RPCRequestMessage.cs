using MessageProtocol;

namespace RPC.Shared.Message
{
    public abstract class ProcedureCallRequestMessage
    {
        public static uint CallId { get; private set; }
        public short MethodId { get; private set; }
        public byte[] ParameterData { get; private set; }
        
        public ProcedureCallRequestMessage(short methodId, byte[] parameterData)
        {
            MethodId = methodId;
            ParameterData = parameterData;
        }
    }

    /// <summary>
    /// 서버에서 클라이언트에게 함수 호출 요청을 할 때 전송하는 메시지
    /// </summary>
    [MessageStandalone(0)]
    public class S_ProcedureCallRequestMessage : ProcedureCallRequestMessage
    {
        public byte HubId { get; private set; }
        
        public S_ProcedureCallRequestMessage(byte hubId, short methodId, byte[] parameterData)
            : base(methodId, parameterData)
        {
            HubId = hubId;
        }
    }
    
    /// <summary>
    /// 클라이언트에서 서버에게 함수 호출 요청을 할 때 전송하는 메시지
    /// </summary>
    [MessageStandalone(1)]
    public class C_ProcedureCallRequestMessage : ProcedureCallRequestMessage
    {
        public byte HubReceiverId { get; private set; }
        
        public C_ProcedureCallRequestMessage(byte hubReceiverId, short methodId, byte[] parameterData)
            : base(methodId, parameterData)
        {
            HubReceiverId = hubReceiverId;
        }
    }
}