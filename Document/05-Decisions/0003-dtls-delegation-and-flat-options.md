---
project: DS_RPC
type: decision
status: accepted
tags: [adr, security, dtls, options]
updated: 2026-09-09
---

# ADR 0003 — DTLS 위임 구조와 평탄 옵션 표면

- 날짜: 2026-09-09
- 상태: 승인(구현 완료 — F13)
- 관련: [[../01-Overview/Feature-Spec|Feature-Spec]] F13, [[../03-Reference/Production-Readiness-Review|Production-Readiness-Review]] 전제 1, 형제 DS_Communication ADR-0009(RUDP TLS·DTLS — 타 저장소 문서, 링크 불가)

## 배경

Production-Readiness-Review 전제 1 "네트워크 경계 설계" — 전송 계층에 기밀성이 없어 공개망 직결이 불가능하다는
판정이었다. Communication 2.5.0이 RUDP 위 DTLS 1.2(BouncyCastle) 옵션을 게시했고, DRPC 는 이를 채택할지만
**공개 표면을 어떻게 노출할지**가 설계 결정으로 남아 있었다(통과 노출 vs 평탄 속성 vs 고수준 인증서 로딩).

## 결정

1. **구현 위임, 표면만 평탄화** — 암호화 구현·인증서 검증·핸드셰이크 게이트는 전부 Communication 계약을 승계한다.
   DRPC 는 `RpcEndpointOptions` 에 BCL 타입만으로 구성된 3개 속성을 추가하고 `ToTransportOptions()` 가
   `RudpTlsOptions` 를 조립해 `RudpTransportOptions.Tls` 로 전달한다(사용자 선택 — 옵션 확장).
2. **역할 분리 속성** — 서버 `ServerCertificate`(`X509Certificate2`), 클라이언트 `TlsTargetHost`(SAN/CN 일치)·
   `TlsCertificateValidation`(핀닝 콜백). 하나의 옵션 객체가 양 역할 필드를 다 실을 수 있으나 각 역할은 자기 필드만
   소비한다. 서버 옵션을 클라이언트에 그대로 복사하면 검증 수단이 없어 **기본 거부**된다(fail-closed — 의도된 계약).
3. **미설정 = 평문** — TLS 필드를 하나라도 설정해야 암호화 모드(양단 일치 필수, 와이어 비호환 — `EnableCrc32c` 와
   동일한 표기 계약). 기본값은 기존 동작·와이어 100% 호환.
4. **세부 노브 미노출** — `HandshakeTimeout`(기본 15초) 등은 Communication 기본값으로 충분하다고 판단해 제외.
   필요해지면 선택 인자 추가로 순수 가산 가능(공개 표면 파괴 없음).
5. **고수준 인증서 로딩 미제공** — PFX/PEM 경로 로딩 헬퍼는 `X509CertificateLoader` 한 줄 수준이라 앱 몫.
   DRPC 가 인증서 저장소 정책(경로·권한·갱신)까지 소유하면 경계가 흐려진다.

## 결과

- E2E 5건(핀닝·TargetHost 왕복, 핀 불일치·검증 수단 없음 거부, 평문 비호환) — 테스트 119 → 124.
- Sandbox `--tls`(서버 자가서명 인증서·지문 출력, 클라 `--tls <지문>` 핀닝) 수동 확인 경로.
- Production-Readiness-Review 전제 1의 **기밀성·서버 인증** 축은 이 옵션으로 해소(클라이언트 인증·세션 토큰은
  여전히 앱 계층 몫 — DTLS 는 클라이언트를 인증하지 않는다).
