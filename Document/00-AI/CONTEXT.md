---
project: DS_RPC
type: context
status: stable
tags: [ai, context]
updated: 2026-08-31
---

# CONTEXT — 에이전트 진입점

재구축 중인 DS_RPC 저장소. 작업 시작 시 이 문서를 먼저 읽는다.

## 현 상태 (2026-08-31)

- **재구축 초기**: `Source/`·`Test/` 미생성, `Sandbox/`는 빈 스켈레톤.
- 구현 범위는 [[../01-Overview/Feature-Spec|Feature-Spec]](레거시 패리티 + 재구축 결정)이 권위 문서.
- 레거시 코드·문서는 `Legacy/` 아카이브. 동작 근거가 필요하면 레거시 문서·코드를 참고하되, **구현 대상은 Feature-Spec**이다.

## 규칙

1. `Source/`, `Test/`, `Sandbox/`, `TemplateSource/` 변경 시 같은 턴에 `Document/` 갱신.
2. 문서 작성 규약은 [[./CONVENTIONS|CONVENTIONS]].
3. 레거시 문서 링크는 **상대 경로 + 별칭** 형식만 사용(단축 링크는 Legacy/Document와 파일명 충돌로 Ambiguous).

## 관련

- [[./CONVENTIONS|CONVENTIONS]]
- [[../01-Overview/Home|Home]]
