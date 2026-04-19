using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator;

internal static class RpcDiagnostics
{
    public static readonly DiagnosticDescriptor HubMustBePartial = new(
        "RPC001",
        "RPC 허브 클래스는 partial 이어야 합니다",
        "클래스 '{0}'는 소스 생성을 위해 partial 로 선언되어야 합니다.",
        "RPC",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnknownHubBase = new(
        "RPC002",
        "지원되지 않는 허브 기본 클래스",
        "클래스 '{0}'의 기본 형식이 RPC.Client.Network.ServerHub<> 또는 RPC.Server.Netwrok.ClientHub<> 가 아닙니다.",
        "RPC",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MissingRemoteProcedure = new(
        "RPC003",
        "[RemoteProcedure] 특성이 필요합니다",
        "원격 프로시저 인터페이스의 메서드 '{0}.{1}'에는 [RemoteProcedure] 특성이 필요합니다.",
        "RPC",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnsupportedReturnType = new(
        "RPC004",
        "지원되지 않는 반환 형식",
        "메서드 '{0}.{1}'의 반환 형식은 현재 RPC 생성기에서 지원하지 않습니다.",
        "RPC",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnsupportedParameterType = new(
        "RPC005",
        "지원되지 않는 매개변수 형식",
        "메서드 '{0}.{1}'의 매개변수 '{2}' 형식은 현재 RPC 생성기에서 지원하지 않습니다.",
        "RPC",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor DuplicateRpcMethodName = new(
        "RPC006",
        "원격 프로시저 이름 충돌",
        "원격 프로시저 메서드 이름 '{0}'이(가) 중복되었습니다. 클라이언트/서버 선언 또는 상속 인터페이스에서 이름이 겹치지 않게 조정하세요.",
        "RPC",
        DiagnosticSeverity.Error,
        true);
}
