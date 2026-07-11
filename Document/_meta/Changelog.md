---
project: DS_RPC
type: overview
status: draft
tags: [changelog]
updated: 2026-07-11
---

# Changelog (Document)

문서 vault 변경만 기록한다. 제품 릴리스 노트는 저장소 릴리스/태그를 따른다.

## 2026-07-11

- Sandbox.Contracts.csproj 훅 동기화: [[Getting-Started]] Sandbox 표에 Contracts-only MP CodeGenerator(`buildtransitive` 없음)·Client/Server DRPC Analyzer 비고 추가; Packages·Scope·FAQ·Configuration과 일치 재확인
- 잔여 이슈 재점검 반영: NuGet CI에서 CodeGenerator↔형제 MP ProjectReference pack 위험, ListenTask 미관찰, MaxConcurrentIncoming setter Dispose; Sandbox.Contracts 참조·`buildtransitive` 없음 재확인; [[Known-Issues]]·[[Packages]]·[[Configuration]]·[[FAQ]]·[[Structure-Performance]] 동기화
- Sandbox.Contracts·CodeGenerator 빌드 문서화: Contracts MP analyzer에서 `buildtransitive` 제거, DRPC.CodeGenerator→형제 MP CodeGenerator ProjectReference/옆 DLL 복사·CA 4.14; [[Packages]]·[[Configuration]]·[[Components]]·[[FAQ]]·[[Structure-Performance]] 동기화
- P0–P3 구현 반영: OneWay CallId=0, CallId 비재사용, MaxConcurrentIncoming, ReliableType 응답, Disconnect/Disconnected, Hub 타임아웃 스캔, async Implementation, RpcListenHandle, Hub alias, `Test/DRPC.Shared.Tests`; ADR [[0001-defer-double-serialization]]·[[0002-defer-netwrok-rename]]; Known-Issues·Public-API·FAQ·Configuration 동기화
- DRPC.CodeGenerator가 형제 `DS_MessageProtocol` CodeGenerator(ProjectReference, CA 4.14)를 옆에 복사해 SDK 10 Roslyn에서 Nested 메시지 생성 `MissingMethodException`/`FileNotFoundException` 회피; Sandbox.Contracts analyzer `buildtransitive` 제거
- [[Structure-Performance]] 추가: 구조·성능·병목(OneWay CallId 누수, 이중 직렬화, Hub 수명, sync Implementation 등)과 P0–P3 해결 로드맵; [[Known-Issues]]·[[Home]]·[[Overview]]·[[FAQ]] 링크
- 패키지 버전 `1.1.0` (`Source/Directory.Build.props`); NuGet 태그 `v1.1.0` 게시
- `Examples/` → `Sandbox/` 폴더 이름 변경; 네임스페이스 `Sandbox.*`; Overview·Packages·Getting-Started·Scope·FAQ·GLOSSARY·README·AGENTS/Rule/Skill 동기화
- Known-Issues 1~6 런타임/생성기 수정 반영: RpcTimeout·CancelPendingCalls·Error msg2·OneWay·명시 MethodId·Obsolete sync·ConnectAsync CT; [[Known-Issues]]·[[Data-Flow]]·[[Public-API]]·[[FAQ]]·[[Configuration]]·[[GLOSSARY]] 동기화
- [[Known-Issues]] 추가: HubBase·생성기 기준 구조·성능·병목·누수 한계와 완화/개선 우선순위; [[FAQ]]·[[Home]] 링크
- 코드 분석 기반 프로젝트·아키텍처 문서 동기화: Overview, Components, Data-Flow, Packages, Public-API, Scope, Configuration, GLOSSARY, Getting-Started, How-To, FAQ
- Document Obsidian Vault 공통 스켈레톤 초기화
