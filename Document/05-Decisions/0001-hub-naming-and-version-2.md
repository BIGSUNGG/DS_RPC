---
project: DS_RPC
type: adr
status: accepted
tags: [adr, naming, version]
updated: 2026-08-31
---

# 0001 — Hub 명명 정렬과 2.0.0 버전 출발

## Status

Accepted (2026-08-31)

## Context

레거시는 Hub 이름이 소유 주체와 어긋나 있었다.

- 클라이언트 앱이 쓰는 Hub가 `ServerHub`(`DRPC.Client.Network`) — "상대 세션 기준" 명명.
- 서버가 쓰는 Hub가 `ClientHub` + 네임스페이스 오타 `DRPC.Server.Netwrok`.
- 완화용으로 `ClientToServerHub`/`ServerToClientHub` 별칭을 추가했으나 온보딩·리뷰 비용이 계속 발생 ([[../../Legacy/Document/02-Architecture/Structure-Performance|Structure-Performance (Legacy)]] §1.1).

재구축은 breaking 변경을 부담 없이 반영할 수 있는 시점이다.

## Decision

1. **소유 주체 기준 명명**: 클라이언트 측 Hub = `ClientHub`(`DRPC.Client.Network`), 서버 측 Hub = `ServerHub`(`DRPC.Server.Network`).
2. 네임스페이스 철자 정정: `Netwrok` → `Network`.
3. 레거시 별칭(`ClientToServerHub`, `ServerToClientHub`)과 구 이름은 제공하지 않는다.
4. 패키지 버전은 **`2.0.0`**에서 출발한다(`Source/Directory.Build.props`).
5. 생성기 진단 DRPCGEN002 메시지와 문서는 새 이름 기준으로 작성한다.

## Consequences

- 레거시 1.x 소비 코드와 소스 호환이 깨진다 — 재구축 특성상 수용.
- 생성기 탐지 대상 베이스 타입·진단·문서·Sandbox·Template을 새 이름으로 작성한다 (권위: [[../01-Overview/Feature-Spec|Feature-Spec]]).
- sync Outgoing 스텁 존폐는 별도 오픈 이슈로 보류 ([[../01-Overview/Feature-Spec|Feature-Spec §오픈 이슈]]).

## 관련

- [[../01-Overview/Feature-Spec|Feature-Spec]]
- [[../../Legacy/Document/05-Decisions/0002-defer-netwrok-rename|0002-defer-netwrok-rename (Legacy)]] — 레거시에서 미뤘던 항목, 재구축으로 해소
