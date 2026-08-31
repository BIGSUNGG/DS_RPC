---
project: DS_RPC
type: adr
status: draft
tags: [adr, api, naming]
updated: 2026-07-11
---

# ADR 0002: Defer Netwrok rename; adopt Hub aliases

## Status

Accepted

## Context

`DRPC.Server.Netwrok` 오타와 `ServerHub`(클라)/`ClientHub`(서버) 명명은 공개 API·생성기(DRPCGEN002)에 고정되어 있다. 즉시 rename은 breaking이다.

## Decision

1. **major(v2) 전까지** `Netwrok` 네임스페이스와 기존 Hub 타입명을 유지한다.
2. 비파괴 별칭을 추가한다:
   - `DRPC.Client.Network.ClientToServerHub<,>` → `ServerHub<,>`
   - `DRPC.Server.Netwrok.ServerToClientHub<,>` → `ClientHub<,>`
3. 생성기는 기존 타입과 alias 모두를 Hub base로 인식한다.

## Consequences

### Positive

- 온보딩 DX 개선, 기존 소비자 코드 무파괴.

### Negative

- 오타 네임스페이스가 당분간 문서·진단에 남는다.

### Neutral

- v2에서 `Network` 철자 수정 및 Hub 의미 정렬을 별도 ADR로 수행한다.

## Alternatives considered

- 즉시 rename + type forward — NuGet 소비자 breaking으로 기각.
- alias만 문서화하고 타입 미추가 — 컴파일 타임 이득 없음.

## 관련

- [[Public-API]]
- [[Structure-Performance]]
- [[FAQ]]
- [[CONTEXT]]
