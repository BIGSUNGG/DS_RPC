---
project: DS_RPC
type: context
status: stable
tags: [ai, pending]
updated: 2026-09-08
---

# PENDING — 보류 사항 기록

사용자 판단이 필요한 사안이나 해결되지 않은 블로커를 기록한다. 해결되면 해당 줄을 지운다.

## [보류 질문] TCP 전송 지원 여부 — 형제 제안 P1 (2026-09-09)

- **무엇**: 형제 DS_Communication `Document/01-Overview/Proposals-Upstream.md` P1 — DS_RPC가 TCP 전송(\+TLS, `TcpTlsOptions`)을 노출할 것인지.
- **왜 보류**: DRPC 재구축 스펙은 전송=RUDP 단일(Feature-Spec·ADR). TCP 추가는 전송 계층 이중화라는 구조 결정 — 루프가 임의로 정할 성격이 아님.
- **임시 조치**: 미채택. 필요 시 형제 ADR-0008(TLS·Schannel 주의사항 포함) 참고해 별도 반복에서 설계.

## [후보] 운영 신호 소비 — 형제 제안 P4 (2026-09-09)

- **무엇**: `ActiveConnectionCount`(TCP·RUDP)·`DisconnectReason.FlowControl` 노출해 앱이 백프레셔·포화 지표로 쓰게 한다.
- **임시 조치**: 미채택 — 후속 반복 후보(P3는 2026-09-09 채택 완료, P2 CRC32c는 v2.5.0 채택 완료).

## [관측] pi-lens LSP CS1061·CS0117·CS1503 — 패키지 버전업 후 메타데이터 캐시 (2026-09-08)

- **무엇**: Communication 2.0.0→2.0.1 버전업 이후 pi-lens LSP 뷰에서만 존재하지 않는 멤버·변환 오류(예: `RudpTransportOptions.ConnectTimeout` 부재, `FakeSession`→`ISession` 변환 불가 — 후자는 버전업 전부터 통과하던 테스트). 언어 서버 재시작 후에도 잔존.
- **왜 캐시 아티팩트인가**: `dotnet build DRPC.slnx -c Release` 0경고/0오류, `-c Debug` 0/0, `dotnet test` 87/87 통과(해당 테스트 전부 포함), `project.assets.json` 2.0.1 해석, 동일 소스 CI 게시 성공. 컴파일러·런타임·CI 전부 수용.
- **임시 조치**: 해당 진단 7건 `lens_diagnostic_mark` false-positive 처리 완료(2026-09-08). 새 버전업 때 재발하면 동일 절차로 기록; 근본 해결(버전업 시 LSP 캐시 리셋)은 pi-lens 쪽 과제.
