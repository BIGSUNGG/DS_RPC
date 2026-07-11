---
project: DS_RPC
type: troubleshoot
status: draft
tags: [troubleshoot, performance, architecture]
updated: 2026-07-11
---

# Known Issues — 구조·성능·병목

코드 기준 한계와 **수정 상태**. 상세는 [[Structure-Performance]].

## 수정됨 (2026-07-11)

| 이슈 | 조치 |
|------|------|
| 수신 sync-over-async | `ProcessRequestAsync` fire-and-forget; Incoming `Task` |
| CallId 할당 레이스 | `Interlocked.Increment` |
| 타임아웃·끊김 pending 누수 | Hub 타임아웃 스캔; `CancelPendingCalls` / `NotifyDisconnected` |
| Implementation 예외 hang | `ProcedureCallErrorMessage` + `RpcFaultException` |
| void 항상 Request/Response | `OneWay` + `SendRPC` |
| MethodId 순서 의존 | 명시 methodId + DRPCGEN004/005 |
| sync Outgoing | Obsolete + Async |
| ConnectAsync CT | 생성기 전달 |
| OneWay CallId 누수 | OneWay CallId **고정 0** |
| CallId 재사용 + 늦은 응답 | CallId **비재사용** |
| Incoming concurrency | `MaxConcurrentIncoming` + `RpcErrorCode.Overloaded` |
| 응답 ReliableType | `MethodReliableTypes` → Response/Error `MessageSendContext` |
| Session Console | 제거; `HubBase.Disconnected` / `Disconnect` / `IDisposable` |
| per-call CTS | Hub `Timer` 스캔으로 대체 |
| Incoming sync Implementation | `partial Task` / `Task<T>` |
| Listen 수명 | `RpcListenHandle` (`IAsyncDisposable`) |
| Hub 명명 혼동 (단기) | `ClientToServerHub` / `ServerToClientHub` alias ([[0002-defer-netwrok-rename]]) |
| `Test/` 부재 | `Test/DRPC.Shared.Tests` |
| Sandbox MP analyzer 이중 로드 | `Sandbox.Contracts`에서 MP CodeGenerator **`buildtransitive` 제거** ([[Packages]]·[[Configuration]]) |

## 잔여 — 운영/CI (우선)

| 이슈 | 영향 | 방향 |
|------|------|------|
| `DRPC.CodeGenerator`가 형제 `DS_MessageProtocol` **ProjectReference** | `.github/workflows/nuget-publish.yml`은 DS_RPC만 checkout → **태그 pack 실패 가능** | CI에서 MP clone, 또는 NuGet MP CodeGenerator로 되돌리기 ([[Configuration]]) |
| `ListenAsync`의 `ListenTask` 미관찰 | `onConnected`/리스너 예외 unobserved; Dispose와 레이스 | 핸들 반환 전/후 `ListenTask` 관측 또는 예외 로깅 |
| `MaxConcurrentIncoming` setter가 사용 중 `SemaphoreSlim` Dispose | 런타임 중 상한 변경 시 `ObjectDisposedException` | 재생성 대신 거부, 또는 드레인 후 교체 |

## 잔여 — 형제·major

| 이슈 | 영향 | 방향 |
|------|------|------|
| Nested + Standalone 이중 직렬화 / ArrayPool | CPU·GC | [[0001-defer-double-serialization]] |
| Communication 동기 `Action<object>` 수신 | awaitable 큐 불가 | DS_Communication |
| `Netwrok` 철자·Hub 의미 정렬 | DX | v2 / [[0002-defer-netwrok-rename]] |
| sync Outgoing 생성 제거 | 블로킹 API 잔존 | major |
| 생성기 스냅샷 테스트 | 회귀 수동(Sandbox) | P1 |
| `RequestRPC` 호출측 `CancellationToken` 없음 | 개별 호출 취소 불가 | Hub API 확장 |
| 타임아웃 1초 Timer 스캔 | 지정 대비 최대 ~1초 지연 | 정밀 휠/타이머 |
| 생성 타입명 `{Method}_Paramter` 오타 | DX (와이어와 무관) | major에서 `Parameter`로 |

## 권장 사용

- Outgoing은 `{Method}Async`만.
- Implementation은 `async Task` / `Task<T>`.
- 알림성 void는 `OneWay = true`.
- 서버는 `await using var handle = await Hub.ListenAsync(...)`; 필요 시 `handle.ListenTask` 관측.
- `MaxConcurrentIncoming`은 **연결 직후·유휴 시**에만 설정.
- 필요 시 `RpcTimeout` 조정.

## 관련

- [[Structure-Performance]]
- [[0001-defer-double-serialization]]
- [[0002-defer-netwrok-rename]]
- [[FAQ]]
- [[Data-Flow]]
- [[Public-API]]
- [[Packages]]
- [[Configuration]]
