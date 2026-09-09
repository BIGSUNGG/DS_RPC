namespace DRPC.Shared;

/// <summary>
/// 생성 디스패치가 <c>_Validate</c> false 로 <c>_Implementation</c> 호출을 건너뛸 때 던진다.
/// 허브가 잡아 <see cref="Message.RpcErrorCode.ValidationFailed"/>(7) 오류 응답으로 바꾼다
/// (one-way 는 응답 채널이 없어 조용히 스킵). 예상된 거부이므로 Unhandled 로 기록되지 않는다.
/// </summary>
public sealed class RpcValidationFailedException(string methodName)
    : SystemException($"The call to {methodName} was rejected by validation (_Validate returned false).");
