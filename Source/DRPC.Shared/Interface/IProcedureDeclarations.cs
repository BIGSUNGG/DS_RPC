namespace DRPC.Shared.Interface;

/// <summary>
/// 서버 측 RPC 계약 인터페이스 마커. 생성기는 이 인터페이스의 <c>[RemoteProcedure]</c> 메서드를
/// 서버 Hub 에서는 Incoming, 클라이언트 Hub 에서는 Outgoing 으로 배정한다.
/// </summary>
public interface IServerProcedureDeclarations
{
}

/// <summary>
/// 클라이언트 측 RPC 계약 인터페이스 마커. 서버 Hub 에서는 Outgoing, 클라이언트 Hub 에서는 Incoming.
/// </summary>
public interface IClientProcedureDeclarations
{
}
