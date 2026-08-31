---
project: DS_RPC
type: adr
status: draft
tags: [adr, serialization, performance]
updated: 2026-07-11
---

# ADR 0001: Defer double-serialization / ArrayPool

## Status

Accepted

## Context

RPC 핫패스는 Nested `[NonIdMessage]` 파라미터를 `byte[]`로 직렬화한 뒤 `ProcedureCallRequestMessage` Standalone에 다시 넣어 이중 직렬화한다. ArrayPool·단일 버퍼 쓰기는 MessageProtocol API와 와이어 호환에 의존한다.

## Decision

이중 직렬화 제거와 ArrayPool 전면 도입은 **DS_MessageProtocol과 합의·버전 업 이후**로 미룬다. DRPC 단독으로는 `Array.Empty` 재사용·빈 페이로드 회피 수준의 완화만 유지한다.

## Consequences

### Positive

- 형제 스택 없이 DRPC P0–P1 안정성 작업을 진행할 수 있다.
- 측정 베이스라인(RPC/s, alloc/call)을 잡은 뒤 최적화할 수 있다.

### Negative

- 대용량 페이로드·고빈도 RPC에서 GC 비용이 당분간 남는다.

### Neutral

- [[Structure-Performance]] §2.1에 잔여로 남긴다.

## Alternatives considered

- DRPC에서 MessageProtocol을 우회하는 커스텀 바이너리 포맷 — 유지보수·중복 증가로 기각.

## 관련

- [[Structure-Performance]]
- [[Known-Issues]]
- [[CONTEXT]]
