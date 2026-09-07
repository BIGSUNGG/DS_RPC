using Communication.Network.RUDP;

namespace DRPC.Shared.Network;

/// <summary>
/// 접속·수신 끝점 전송 옵션 묶음 — <see cref="HubSessionFactory.CreateTransportOptions(RpcEndpointOptions)"/> 로
/// 전송 스택 옵션으로 변환된다. 연결 타임아웃·연결 상한과 달리 끝단 공유값이므로 묶음 타입으로 제공한다.
/// </summary>
public sealed class RpcEndpointOptions
{
    /// <summary>접속 키. null/빈 문자열이면 전송 스택 기본 키.</summary>
    public string? ConnectionKey { get; set; }

    /// <summary>
    /// 클라이언트 연결 시도 상한(ms). 침묵 호스트(블랙홄) 연결 실패를 이 시간 이내로 확정한다.
    /// 0(기본)이면 전송 스택 기본값(약 5초). 음수는 거부.
    /// </summary>
    public int ConnectTimeoutMs { get; set; }

    /// <summary>
    /// 서버 동시 수락 연결 상한. 상한 도달 시 접속 요청은 즉시 거부되고 수락은 계속된다(연결 고갈 공격 방어).
    /// 0(기본)이면 무제한. 음수는 거부.
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    /// 패킷 무결성 검사(CRC32c) 활성화 — 송신마다 체크섬(4바이트)을 붙이고 수신은 위반 패킷을 프로토콜 처리 전에 폐기한다
    /// (Communication <c>RudpTransportOptions.Crc32cEnabled</c>). <b>양단 모두 같은 설정</b>이어야 한다(와이어 비호환).
    /// 위변조 검출뿐 방지가 아니다(키 없는 CRC — 능동 공격자는 재계산 가능). 기밀성·인증은 없다. 기본 <c>false</c>.
    /// </summary>
    public bool EnableCrc32c { get; set; }

    /// <summary>
    /// 전송 스택 옵션으로 변환한다 — 필드별 계약(음수 거부·0=미설정)은 <see cref="HubSessionFactory.CreateTransportOptions"/> 매개변수 버전과 동일.
    /// </summary>
    public RudpTransportOptions ToTransportOptions()
        => HubSessionFactory.CreateTransportOptions(
            ConnectionKey, ConnectTimeoutMs, MaxConnections, EnableCrc32c);
}
