using MessageProtocol;

namespace RPC.Shared.Message
{
    [StandaloneMessage(0)]
    public partial class ProcedureCallRequestMessage
    {
        public uint CallId { get; private set; }
        public int MethodId { get; private set; }
        public byte[] ParameterData { get; private set; }
        
        public ProcedureCallRequestMessage() { }

        public ProcedureCallRequestMessage(uint callId, int methodId, byte[] parameterData)
        {
            CallId = callId;
            MethodId = methodId;
            ParameterData = parameterData;
        }
    }
}