---
project: DS_RPC
type: overview
status: stable
tags: [scope, spec, feature]
updated: 2026-09-05
---

# Feature Spec — 재구축 구현 기능 명세

레거시(`Legacy/`)가 지원한 기능 스펙을 새 프로젝트에서 이어 구현한다.
이 문서는 **구현할 기능의 범위·동작·수용 기준**을 정의한다.
레거시 동작 근거는 아카이브 문서: [[../../Legacy/Document/03-Reference/Public-API|Public-API (Legacy)]], [[../../Legacy/Document/02-Architecture/Data-Flow|Data-Flow (Legacy)]], [[../../Legacy/Document/06-Troubleshooting/Known-Issues|Known-Issues (Legacy)]].

## 구현 상태 (2026-09-05)

F1–F7·F9·F11 **구현 완료**(`dotnet test DRPC.slnx -c Release` 53개 통과). F8(게시 CI)·F10(Template)은 이번 범위 밖.
형제 스택은 NuGet **2.0.0** 안정판으로만 참조한다(형제 저장소 소스 참조 없음).

## 원칙

1. **레거시 패리티 우선**: 아래 기능은 레거시와 동일한 동작이 기준이다. 단 §재구축 결정에서 확정한 항목은 새 설계가 기준이다.
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

- `[RemoteProcedure(RpcDeliveryMode mode = ReliableOrdered, int methodId = -1)]`, `OneWay` named 프로퍼티.
- 계약 마커 인터페이스: `IServerProcedureDeclarations` / `IClientProcedureDeclarations` (Shared).
- 메서드별 전송 방식(DRPC 자체 `RpcDeliveryMode`)을 계약에서 고정 — 전송 스택 열거형은 계약면에 노출하지 않는다(ADR-0002 결정 2).

### 수용 기준

- Attribute·enum **두 타입만으로** 계약이 완결되고 `DRPC.Attribute` 의 패키지 의존은 0이다.
- `methodId` 미지정 시 생성기가 DRPCGEN004 경고로 보완한다 (F5).
- 함정: 첫 positional 인자는 `mode` 다. `[RemoteProcedure(0)]` 은 methodId 0 이 아니라 `Unreliable`.

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
- **응답·오류의 전송 방식은 요청 MethodId 에 등록된 방식(`MethodDeliveryModes`)을 따른다.**
- **one-way 신호는 `CallId == 0`** — 수신 측 등록표로 판정하지 않는다(ADR-0002 결정 3).

### 수용 기준

- Unknown MethodId(non-one-way) → `UnknownMethod`; Implementation 예외 → `Unhandled`; 상한 초과 → `Overloaded`.
- `Timeout`·`Disconnected` 는 와이어 코드로 전송되지 않고 호출 측 `TimeoutException`·`InvalidOperationException` 로만 관찰된다(코드는 정의만 유지).
- 페이로드 이중 직렬화 제거는 **이번 범위 밖** ([[../../Legacy/Document/05-Decisions/0001-defer-double-serialization|ADR-0001 (Legacy)]] 유지).

## F4 — 메시지 핸들러 라우팅 (`DRPCMessageHandler`)

- 세션 수신 메시지 → 요청/응답/에러별 라우팅.
- 세션 끊김 시 `NotifyDisconnected` → pending 실패 처리와 연결.
- 라이브러리에 `Console.WriteLine` 등 콘솔 의존 금지(이벤트/콜백만).

## F5 — 코드 생성기 (`DRPC.CodeGenerator`)

Roslyn incremental generator. `partial` Hub + Hub 베이스 상속을 탐지해 스텁 생성.

- **탐지**: 클라 `ClientHub`, 서버 `ServerHub` 상속 partial class (명명 정렬 — [[../05-Decisions/0001-hub-naming-and-version-2|ADR-0001]]).
- **Outgoing**: `{Method}Async` **만** 생성(sync `[Obsolete]` 스텁 없음 — ADR-0002 결정 1). OneWay → `SendRPC`, 아니면 `RequestRPC`.
- **Incoming**: `async Task<byte[]>` 디스패치(`{Name}_Requested`) + 사용자 `partial Task` / `Task<T>` `{Name}_Implementation`.
- **연결**: 클라 `ConnectAsync(host, port, connectionKey?, ct)`, 서버 `ListenAsync(port, connectionKey?, onConnected, ct)` → `RpcListenHandle`.
- **페이로드**: 메서드별 래퍼 메시지 타입 없이, 매개변수·반환을 `MessageBufferWriter` 에 선언 순서로 이어 붙인다(ADR-0002 결정 4).
  메시지 타입 값만 MessageProtocol 에 위임한다 — `[NonIdMessage]` 은 타입 고정 `Serialize<T>`/`Deserialize<T>`,
  ID 헤더 메시지(Standalone/Group/Generic)는 `SerializeToWriter`/`DeserializeFromReader`(그룹 다형성 보존).
- **진단**: DRPCGEN001 partial / 002 Hub 베이스 / 003 지원 타입·오버로드·`Task` 반환 / 004 명시 methodId 권장(warning) / 005 중복 methodId(error) / 006 OneWay+non-void(error).
  DRPCGEN001 은 실제로 발화해야 하므로 생성기 파이프라인 전제조건에서 `partial` 을 빼고(베이스명에 `Hub` 포함만으로 후보 선별) 판정한다.

### 수용 기준

- 생성 단계: 탐지→수집→연결 배선→Outgoing/Incoming 스텁→페이로드 헬퍼. (레거시 6단계 중 "래퍼 타입 직렬화 생성"단계가 페이로드 인라인으로 흡수됨)
- 서버 Hub 기준 Outgoing=ClientDecls·Incoming=ServerDecls(클라는 반대).
- `DRPC.CodeGenerator` 는 `Microsoft.CodeAnalysis.CSharp` 만 참조한다 — MessageProtocol 생성기·형제 저장소 경로를 참조하지 않는다(F8 잔여 이슈의 구조적 해소).

## F6 — 클라이언트 Hub·연결 (`DRPC.Client`)

- Hub 베이스 `ClientHub<TSPD, TCPD>`(`DRPC.Client.Network`) + `RpcClient.ConnectAsync` — 2.0.0 의 `RudpConnector` 가 `IMessageChannel` 을 노출한다.
- 세션 어댑터 클래스는 두지 않는다: 2.0.0 `RudpSession(channel, converter, handlerFactory)` 을 `HubSessionFactory.CreateRudpSession` 이 조립한다. `LiteNetLib` 직접 참조 없음.
- 실패 시 `InvalidOperationException("Failed to connect to server.")`.

## F7 — 서버 Hub·리스너 수명 (`DRPC.Server`)

- Hub 베이스 `ServerHub<TSPD, TCPD>`(`DRPC.Server.Network`) + `RpcHost.ListenAsync`(2.0.0 `RudpListener.Accepted`); peer마다 Hub 인스턴스 + `onConnected(hub)` 콜백(예외는 `Trace` 로 격리, 콘솔 의존 없음).
- `ListenAsync` → `Task<RpcListenHandle>`(`IAsyncDisposable`): 리스너 Stop/Dispose + CT cancel, 등록 peer 정리.
- `RpcListenHandle.ListenTask`는 **관찰 가능하게** 노출/완료 보장(레거시 미관찰 이슈 해소).

## F8 — NuGet 패키징·게시 (부분: 게시 CI 는 범위 밖)

- `Source/` 5개 라이브러리, TFM `netstandard2.1`(CodeGenerator 는 netstandard2.0), `IsPackable=true` (CodeGenerator는 `DevelopmentDependency`).
- 버전은 루트 `Directory.Build.props`(`MessageProtocolPackageVersion`·`CommunicationPackageVersion` 포함).
- 태그 `v*` → GitHub Actions pack·publish.

### 수용 기준

- 레거시 잔여 이슈였던 **CodeGenerator↔MessageProtocol CI pack 실패**가 재발하지 않는 구조 — 생성기가 형제 저장소 경로를 전혀 참조하지 않으므로 저장소 단독 pack 이 성립한다.
- 태그 `v*` → GitHub Actions publish 워크플로는 **미구현**(범위 밖).

## F9 — Sandbox 데모 (`Sandbox/*`, 비배포)

- `Sandbox.Contracts`(계약·메시지) / `Sandbox.Server` / `Sandbox.Client`, TFM net10.0.
- Analyzer 규칙: `MessageProtocol` 패키지 참조는 `Sandbox.Contracts` 만 한다(NuGet 기본 PrivateAssets 가 analyzers 를 전파하지 않음 → Client/Server 로 안 퍼진다). Client/Server 는 `DRPC.CodeGenerator` 를 `OutputItemType=Analyzer` 로만 참조.
- 기본: `127.0.0.1:9050`, key `sandbox-key`. 데모 내용: 기본 ReliableOrdered 호출, `Sequenced`/`ReliableUnordered` 오버라이드, OneWay, DTO(NonId) 왕복, 양방향 역호출, 그룹 다형성(`ShoutChatLine`), stdin 종료.

## F10 — Template (`TemplateSource/`, 비배포)

- 계약·Hub·생성기 사용 최소 템플릿(앱 스캐폴드 용도).

## F11 — 테스트 인프라 (`Test/*`)

| 단계 | 프로젝트 | 내용 | 결과 |
| ------ | ---------- | ------ | ------ |
| 단위 (P0) | `Test/DRPC.Shared.Tests` | CallId·타임아웃·동시성 상한·오류 3종·one-way(CallId 0)·전송 매핑·핸들러 라우팅 | 19 통과 |
| 생성기 (P1) | `Test/DRPC.CodeGenerator.Tests` | DRPCGEN001–006, 생성 형태(Async 전용·`[Obsolete]` 금지·페이로드 인코딩·NonId/Group 경로), 생성 결과 컴파일 | 22 통과 |
| 루프백 E2E (P2) | `Test/DRPC.E2E.Tests` | 실제 RUDP(127.0.0.1) 왕복: 기본/오버라이드 전송·OneWay·DTO·양방향·UnknownMethod·Unhandled·타임아웃·ListenTask/끊김 | 12 통과 |

---

## 재구축 결정 (레거시 잔여의 자연 해소)

재구축이므로 레거시가 호환 때문에 미룬 항목을 **지금 확정**한다. 이견 시 ADR(`Document/05-Decisions/`)로 기록.

| 레거시 잔여 | 재구축 결정 |
| ------------- | ------------- |
| `DRPC.Server.Netwrok` 오타 | `Network`로 철자 정정 (신규 네임스페이스) |
| Hub 명명 혼동 (클라=ServerHub 등) | **결정(2026-08-31)**: 소유 주체 기준 정렬 — 클라 측 `ClientHub`(DRPC.Client), 서버 측 `ServerHub`(DRPC.Server). 레거시 별칭 제거 |
| 전송 스택의 신뢰도 enum 을 계약에 재사용 | **결정(2026-09-05)**: `DRPC.RpcDeliveryMode` 자체 정의 + `RpcDeliveryMap` name-based 매핑 — 2.0.0 에서 개칭·이동된 외부 열거형에 계약을 묶지 않는다(구 이름은 ADR-0002 에 기록) |
| 메서드별 래퍼 메시지 타입(`{Method}_Paramter`) | **결정(2026-09-05)**: 래퍼 타입 폐기, flat 페이로드 인코딩(오타 교정 자체가 무효화됨) |
| sync Outgoing `[Obsolete]` 생성 | **결정(2026-09-05)**: Async 전용 — 오픈 이슈 1 해소 |
| one-way 를 수신 측 등록표로 판정 | **결정(2026-09-05)**: `CallId == 0` 만 신호, `OneWayMethodIds` 폐기 |
| Template 계약 파일명 오타 | 보류(F10 범위 밖) |
| 버전 `1.1.0` | **결정(2026-08-31)**: `2.0.0` 출발 |

근거: [[../05-Decisions/0001-hub-naming-and-version-2|ADR-0001]] (명명·버전), [[../05-Decisions/0002-async-only-delivery-and-payload|ADR-0002]] (스텁·전송·one-way·페이로드)

## 범위 밖

- 저수준 소켓/전송 구현 (→ DS_Communication)
- 범용 직렬화 엔진 (→ DS_MessageProtocol)
- NuGet 게시 CI(F8 의 `v*` 워크플로)·TemplateSource(F10)
- 이중 직렬화·버퍼 풀링 최적화 (측정 후, 형제 합의 필요)
- Communication 수신 핸들러 async 화 (형제 프로젝트 영역)
- 페이로드 와이어 포맷의 버전 간 호환(단방향 인코딩만 보장)

## 오픈 이슈

없음 — 레거시로부터 넘겨받은 오픈 이슈 1(sync Outgoing 스텁 존폐)은 **Async 전용**으로 확정 해소했다(ADR-0002 결정 1).

## 관련

- [[./Home|Home]]
- [[../02-Architecture/Overview|Architecture Overview]] · [[../03-Reference/Public-API|Public-API]] · [[../04-Guides/Getting-Started|Getting-Started]]
- [[../06-Troubleshooting/Known-Issues|Known-Issues]]
- [[../../Legacy/Document/01-Overview/Scope|Scope (Legacy)]]
- [[../../Legacy/Document/02-Architecture/Overview|Architecture Overview (Legacy)]]
- [[../_meta/Changelog|Changelog]]
