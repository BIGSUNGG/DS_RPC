using MessageProtocol;

namespace RPC.Shared.Message
{
    [MessageStandalone(0)]
    public class ProcedureCallRequestMessage
    {
        public static uint CallId { get; private set; }
        public int MethodId { get; private set; }
        public byte[] ParameterData { get; private set; }
        
        public ProcedureCallRequestMessage(uint callId, int methodId, byte[] parameterData)
        {
            CallId = callId;
            MethodId = methodId;
            ParameterData = parameterData;
        }
    }
}