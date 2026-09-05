---
project: DS_RPC
type: meta
status: stable
tags: [meta, changelog]
updated: 2026-08-31
---

# Changelog

문서 변경 기록(최신 위). 코드 변경은 커밋 메시지로 추적한다.

## 2026-09-05

- 재구축 F1–F7·F9·F11 구현 완료에 맞춰 문서 신규: `02-Architecture/Overview.md`(패키지 그래프·호출 흐름·페이로드 인코딩),
  `03-Reference/Public-API.md`(표면·NuGet 2.0.0 고정·오류 코드·진단·빌드 명령), `04-Guides/Getting-Started.md`,
  `06-Troubleshooting/Known-Issues.md`.
- `05-Decisions/0002-async-only-delivery-and-payload.md` 신규: Async 전용 스텁, DRPC 자체 `RpcDeliveryMode` + 매핑 경계,
  one-way = `CallId 0`, 메서드별 래퍼 타입 폐기(flat 페이로드). 4결정 기록.
- `01-Overview/Feature-Spec.md` 갱신: 위 결정을 F1·F3·F5·F6·F7·F8·F9·F11 에 반영, 구현 상태 절 추가(테스트 53개),
  오픈 이슈 1 해소로 "없음", 스테일했던 전송 열거형 명세 제거, `MethodReliableTypes` → `MethodDeliveryModes` 개칭 반영.
- `00-AI/CONTEXT.md` 현 상태 갱신(초기 스켈레톤 → 구현 완료, `-c Release` 사용 이유, 빌드·테스트·샌드박스 명령),
  `01-Overview/Home.md` MoC 확장.

## 2026-08-31

- `05-Decisions/0001-hub-naming-and-version-2.md` 신규: Hub 명명 소유 주체 기준 정렬(`ClientHub`/`ServerHub`) + `2.0.0` 출발 결정.
- `01-Overview/Feature-Spec.md` 갱신: 명명·버전 결정 반영, sync Outgoing은 오픈 이슈로 보류.
- `01-Overview/Feature-Spec.md` 신규: 레거시 패리티 기반 재구축 기능 명세(F1–F11, 재구축 결정, 오픈 이슈).
- `01-Overview/Home.md` 신규: 사람용 진입점(스텁).
- `00-AI/CONTEXT.md`, `00-AI/CONVENTIONS.md` 신규: 에이전트 진입점·작성 규약(레거시 승계).
