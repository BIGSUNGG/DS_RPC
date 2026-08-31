---
project: DS_RPC
type: overview
status: draft
tags: [scope, spec, feature]
updated: 2026-08-31
---

# Feature Spec — 재구축 구현 기능 명세

레거시(`Legacy/`)가 지원한 기능 스펙을 새 프로젝트에서 이어 구현한다.
이 문서는 **구현할 기능의 범위·동작·수용 기준**을 정의한다.
레거시 동작 근거는 아카이브 문서: [[../../Legacy/Document/03-Reference/Public-API|Public-API (Legacy)]], [[../../Legacy/Document/02-Architecture/Data-Flow|Data-Flow (Legacy)]], [[../../Legacy/Document/06-Troubleshooting/Known-Issues|Known-Issues (Legacy)]].

## 원칙

1. **레거시 패리티 우선**: 아래 기능은 레거시와 동일한 동작이 기준이다.
2. **재구축으로 자연 해소되는 항목**은 새 이름/설계로 바로 반영한다 (§ 재구축 결정).
3. **형제 프로젝트 경계 유지**: 전송 = DS_Communication(RUDP), 직렬화 = DS_MessageProtocol. 직접 구현하지 않는다.
4. 수용 기준 미달 기능은 완료로 보지 않는다.

---

## 기능 목록

| ID | 기능 | 패키지 | 우선 |
| ---- | ------ | -------- | ------ |
| F1 | Attribute RPC 계약 | DRPC.Attribute | P0 |
| F2 | Hub 런타임 (HubBase) | DRPC.Shared | P0 |
| F3 | 와이어 메시지·오류 모델 | DRPC.Shared | P0 |
| F4 | 메시지 핸들러 라우팅 | DRPC.Shared | P0 |
| F5 | 코드 생성기 (Hub 스텁) | DRPC.CodeGenerator | P0 |
| F6 | 클라이언트 Hub·연결 | DRPC.Client | P1 |
| F7 | 서버 Hub·리스너 수명 | DRPC.Server | P1 |
| F8 | NuGet 패키징·게시 | Source/* | P1 |
| F9 | Sandbox 데모 | Sandbox/* | P2 |
| F10 | Template | TemplateSource/* | P2 |
| F11 | 테스트 인프라 | Test/* | P0–P2 |

---

## F1 — Attribute RPC 계약 (`DRPC.Attribute`)

인터페이스 메서드에 원격 호출 계약을 표시한다.

- `[RemoteProcedure(ReliableType type, int methodId = -1)]`, `OneWay` named 프로퍼티.
- 계약 마커 인터페이스: `IServerProcedureDeclarations` / `IClientProcedureDeclarations` (Shared).
- 메서드별 전송 신뢰도(`ReliableType`)를 계약에서 고정.

### 수용 기준

- Attribute만으로 계약 선언이 완결되고, 런타임 의존은 `Communication.Network.RUDP.Shared`(`ReliableType`)만.
- `methodId` 미지정 시 생성기가 경고로 보완한다 (F5).

## F2 — Hub 런타임 (`HubBase`, DRPC.Shared)

RPC 호출의 실질 런타임 전부.

- **Outgoing**: `RequestRPC`(응답 대기, TCS) / `SendRPC`(one-way, **CallId 고정 0**).
- **CallId**: `Interlocked` 단조 증가, **비재사용**. 0은 one-way 예약.
- **타임아웃**: `RpcTimeout`(기본 30s, `<= Zero`/Infinite면 무제한), Hub 공용 타이머 스캔(per-call CTS 없음).
- **Incoming**: 수신 → 비블로킹 처리 → 응답. `MaxConcurrentIncoming`(0=무제한) 세마포어; 초과 시 요청은 `Overloaded` 오류, one-way는 drop.
- **수명**: `Disconnected` 이벤트, `Disconnect()`, `IDisposable`; 끊김 시 `CancelPendingCalls`로 pending TCS 예외 완료.

### 수용 기준

- 응답 지연·중복 CallId는 대기 Task 없으면 무시(잘못된 완료가 없어야 함).
- `MaxConcurrentIncoming`은 **연결 직후·유휴 시에만** 설정하도록 문서화(사용 중 변경은 미지원).

## F3 — 와이어 메시지·오류 모델 (DRPC.Shared)

| MessageId | 타입 | 필드 |
| ----------- | ------ | ------ |
| 0 | `ProcedureCallRequestMessage` | `CallId`, `MethodId`, `ParameterData` |
| 1 | `ProcedureCallResponseMessage` | `CallId`, `ReturnData` |
| 2 | `ProcedureCallErrorMessage` | `CallId`, `ErrorCode`, `Message` |

- `[StandaloneMessage]` 등록, MessageProtocol 직렬화.
- `RpcErrorCode`: `Unhandled` / `UnknownMethod` / `Timeout` / `Disconnected` / `Overloaded`.
- 원격 오류는 호출측에서 `RpcFaultException`으로 관찰.
- **응답/에러의 ReliableType은 요청의 Incoming MethodId 맵(`MethodReliableTypes`)을 따른다.**

### 수용 기준

- Unknown MethodId(non-one-way) → `UnknownMethod`; Implementation 예외 → `Unhandled`.
- 페이로드 이중 직렬화 제거는 **이번 범위 밖** ([[../../Legacy/Document/05-Decisions/0001-defer-double-serialization|ADR-0001 (Legacy)]] 유지).

## F4 — 메시지 핸들러 라우팅 (`DRPCMessageHandler`)

- 세션 수신 메시지 → 요청/응답/에러별 라우팅.
- 세션 끊김 시 `NotifyDisconnected` → pending 실패 처리와 연결.
- 라이브러리에 `Console.WriteLine` 등 콘솔 의존 금지(이벤트/콜백만).

## F5 — 코드 생성기 (`DRPC.CodeGenerator`)

Roslyn incremental generator. `partial` Hub + Hub 베이스 상속을 탐지해 스텁 생성.

- **탐지**: 클라 `ClientHub`, 서버 `ServerHub` 상속 partial class (명명 정렬 — [[../05-Decisions/0001-hub-naming-and-version-2|ADR-0001]]).
- **Outgoing**: `[Obsolete]` sync + `{Method}Async` 생성. OneWay → `SendRPC`, 아니면 `RequestRPC`.
- **Incoming**: `async Task<byte[]>` 디스패치 + 사용자 `partial Task` / `Task<T>` `{Name}_Implementation`.
- **연결**: 클라 `ConnectAsync(host, port, connectionKey?, ct)`, 서버 `ListenAsync(port, connectionKey?, onConnected, ct)` → `RpcListenHandle`.
- **직렬화**: nested 메시지 타입(`[NonIdMessage]` 래퍼) + MessageProtocol 생성 코드.
- **진단**: DRPCGEN001 partial / 002 Hub 베이스 / 003 지원 타입 / 004 명시 methodId 권장(warning) / 005 중복 methodId(error) / 006 OneWay+non-void(error).

### 수용 기준

- 레거시 생성 파이프라인 6단계(탐지→수집→Outgoing→Incoming→연결→직렬화)가 동일하게 재현.
- 서버 Hub 기준 Outgoing=ClientDecls·Incoming=ServerDecls(클라는 반대).

## F6 — 클라이언트 Hub·연결 (`DRPC.Client`)

- Hub 베이스(클라 측) + `ServerSession`(RUDP 클라이언트 어댑터).
- `ConnectAsync` → `RUDPConnector` → Hub + 세션 + 핸들러 조립. 실패 시 `InvalidOperationException("Failed to connect to server.")`.

## F7 — 서버 Hub·리스너 수명 (`DRPC.Server`)

- Hub 베이스(서버 측) + `ClientSession`; peer마다 Hub 인스턴스 + `onConnected(hub)` 콜백.
- `ListenAsync` → `Task<RpcListenHandle>`(`IAsyncDisposable`): 리스너 Stop/Dispose + CT cancel, 등록 peer 정리.
- `RpcListenHandle.ListenTask`는 **관찰 가능하게** 노출/완료 보장(레거시 미관찰 이슈 해소).

## F8 — NuGet 패키징·게시

- `Source/` 5개 라이브러리, TFM `netstandard2.1`, `IsPackable=true` (CodeGenerator는 `DevelopmentDependency`).
- 버전은 루트 `Directory.Build.props`(`MessageProtocolPackageVersion`·`CommunicationPackageVersion` 포함).
- 태그 `v*` → GitHub Actions pack·publish.

### 수용 기준

- 레거시 잔여 이슈였던 **CodeGenerator↔MessageProtocol CI pack 실패**가 재발하지 않는 구조(형제 저장소 checkout 또는 NuGet 참조로 고정).

## F9 — Sandbox 데모 (`Sandbox/*`, 비배포)

- `Sandbox.Contracts`(계약·메시지) / `Sandbox.Server` / `Sandbox.Client`, TFM net10.0.
- Analyzer 규칙 유지: **MP CodeGenerator는 Contracts만**(`PrivateAssets=all`, `IncludeAssets`에 `buildtransitive` 없음 → Client/Server로 미전파), Client/Server는 DRPC.CodeGenerator만.
- 기본: `127.0.0.1:9050`, key `sandbox-key`. 양방향 RPC + OneWay + 정상 종료 예제.

## F10 — Template (`TemplateSource/`, 비배포)

- 계약·Hub·생성기 사용 최소 템플릿(앱 스캐폴드 용도).

## F11 — 테스트 인프라 (`Test/*`)

| 단계 | 내용 | 우선 |
| ------ | ------ | ------ |
| HubBase 단위 테스트 (xUnit) | CallId·타임아웃·동시성·오류 경로 | P0 |
| 생성기 스냅샷 테스트 | DRPCGEN001–006 + 출력 회귀 | P1 |
| 헤드리스 통합 | Sandbox loopback RUDP E2E | P2 |

---

## 재구축 결정 (레거시 잔여의 자연 해소)

재구축이므로 레거시가 호환 때문에 미룬 항목을 **지금 확정**한다. 이견 시 ADR(`Document/05-Decisions/`)로 기록.

| 레거시 잔여 | 재구축 결정 |
| ------------- | ------------- |
| `DRPC.Server.Netwrok` 오타 | `Network`로 철자 정정 (신규 네임스페이스) |
| Hub 명명 혼동 (클라=ServerHub 등) | **결정(2026-08-31)**: 소유 주체 기준 정렬 — 클라 측 `ClientHub`(DRPC.Client), 서버 측 `ServerHub`(DRPC.Server). 레거시 별칭 제거 |
| `{Method}_Paramter` 생성 타입 오타 | `Parameter`로 정정 |
| sync Outgoing `[Obsolete]` 생성 | **보류** — 오픈 이슈 참조 |
| Template 계약 파일명 오타 | 정정 |
| 버전 `1.1.0` | **결정(2026-08-31)**: `2.0.0` 출발 |

명명·버전 결정 근거: [[../05-Decisions/0001-hub-naming-and-version-2|ADR-0001]]

## 범위 밖

- 저수준 소켓/전송 구현 (→ DS_Communication)
- 범용 직렬화 엔진 (→ DS_MessageProtocol)
- 이중 직렬화·버퍼 풀링 최적화 (측정 후, 형제 합의 필요)
- Communication 수신 핸들러 async 화 (형제 프로젝트 영역)

## 오픈 이슈 (결정 필요)

1. **sync Outgoing 스텁 존폐**: 레거시 생성기는 계약 메서드마다 `[Obsolete]` sync 스텁과 `{Method}Async`를 함께 생성한다. sync 스텁은 `RequestRPC(...).GetAwaiter().GetResult()` 블로킹 구현(스레드 점유·데드락 위험 — 상세 검토 후 결정).
   - A) 완전 제거 (`{Method}Async`만 생성)
   - B) `[Obsolete]` 유지하며 생성 (레거시 호환)
   - C) opt-in 플래그 시에만 생성

## 관련

- [[./Home|Home]]
- [[../../Legacy/Document/01-Overview/Scope|Scope (Legacy)]]
- [[../../Legacy/Document/02-Architecture/Overview|Architecture Overview (Legacy)]]
- [[../_meta/Changelog|Changelog]]
