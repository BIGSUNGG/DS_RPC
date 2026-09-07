---
project: DS_RPC
type: context
status: stable
tags: [ai, pending]
updated: 2026-09-08
---

# PENDING — 보류 사항 기록

사용자 판단이 필요한 사안이나 해결되지 않은 블로커를 기록한다. 해결되면 해당 줄을 지운다.

## [관측] pi-lens LSP 오류 CS1061 — RudpTransportOptions.ConnectTimeout 부재 (2026-09-08)

- **무엇**: `Source/DRPC.Shared/Network/HubSessionFactory.cs` L45의 `options.ConnectTimeout = connectTimeoutMs;` 가 pi-lens LSP 뷰에서만 CS1061(멤버 없음)으로 보고된다.
- **왜 캐시 아티팩트인가**: Communication 패키지 2.0.0→2.0.1 버전업 직후 발생. ① `dotnet build DRPC.slnx -c Release` 0경고/0오류(2회), ② `-c Debug` 단일 프로젝트 빌드 0/0, ③ `project.assets.json` 이 `Communication.Network.RUDP.Shared/2.0.1` 해석, ④ `ConnectTimeout` 계약을 직접 검증하는 단위 테스트 5건 통과, ⑤ 동일 소스로 CI(NuGet Publish run 34148443218) 팩·푸시 성공. 컴파일러·런타임·CI 전부 수용 — 인프로세스 언어 서버의 구(舊) 2.0.0 메타데이터 그래프 캐시가 원인.
- **임시 조치**: 코드 수정 불필요(고칠 결함이 아님). 언어 서버 재시작 후 진단이 소멸하는지만 다음 반복에서 확인하고, 남아 있으면 pi-lens 캐시 리셋(`lens doctor`) 검토.

## [관측] NuGet flatcontainer nupkg blob 유입 지연 (2026-09-08)

- **무엇**: v2.2.0 게시 직후 5개 패키지의 flatcontainer **인덱스**에는 2.2.0 등장(전부 확인)했으나 nupkg blob 직접 GET이 잠시 404.
- **임시 조치**: 인덱스 등재 + Actions 푸시 성공(실패 시 `-euo pipefail` 로 잡 실패)이 게시 수용의 권위 신호이므로 완료로 판정. 다음 반복에서 nupkg blob 200 확인 후 이 줄 삭제.
