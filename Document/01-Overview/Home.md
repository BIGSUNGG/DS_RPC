---
project: DS_RPC
type: overview
status: stable
tags: [moc, home]
updated: 2026-09-05
---

# DS_RPC (DRPC) — Home

분산 RPC 및 RUDP 기반 통신을 위한 .NET 라이브러리. 재구축 진행 중.
기존 코드·문서는 `Legacy/`에 아카이브되어 있다.

## Map of Content

### AI

- [[../00-AI/CONTEXT|CONTEXT]] — 에이전트 진입점
- [[../00-AI/CONVENTIONS|CONVENTIONS]]

### Overview

- [[Feature-Spec]] — 재구축 구현 기능 명세 (권위)

### Architecture · Reference · Guides

- [[../02-Architecture/Overview|Architecture Overview]] — 패키지 그래프·호출 흐름·페이로드 인코딩
- [[../03-Reference/Public-API]] — 표면·패키지·진단·빌드 명령
- [[../04-Guides/Getting-Started]] — RPC 선언하고 호출하기
- [[../06-Troubleshooting/Known-Issues|Known-Issues]] — 함정·한계·빌드 이슈

### Decisions (ADR)

- [[../05-Decisions/0001-hub-naming-and-version-2|0001]] — Hub 명명 정렬·2.0.0 출발
- [[../05-Decisions/0002-async-only-delivery-and-payload|0002]] — Async 전용 스텁·자체 전송 열거형·CallId 0 one-way·flat 페이로드

### Meta

- [[../_meta/Changelog|Changelog]]

## 상태

F1–F9·F11 구현 완료(테스트 58개 통과)·`v2.0.0` NuGet 게시 확인. F10 Template 는 범위 밖.

## 아카이브

레거시 문서는 `Legacy/Document/`(Home: [[../../Legacy/Document/01-Overview/Home|Home (Legacy)]]).
