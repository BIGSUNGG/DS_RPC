---
project: DS_RPC
type: troubleshoot
status: draft
tags: [troubleshoot, performance, architecture]
updated: 2026-07-11
---

# Known Issues — 구조·성능·병목

코드 기준 한계와 **수정 상태**. 잔여 항목만 운영 전제로 둔다.

## 수정됨 (2026-07-11)

| 이슈 | 조치 |
|------|------|
| 수신 sync-over-async (`GetAwaiter().GetResult`) | `ProcessRequestAsync` fire-and-forget; Incoming `Func<byte[], Task<byte[]>>` |
| CallId 할당 레이스 | `Interlocked.Increment` |
| 타임아웃·끊김 pending 누수 | `HubBase.RpcTimeout`(기본 30s); `CancelPendingCalls` + `DRPCMessageHandler.OnDetectedDisconnection` |
| Implementation 예외 시 hang | `ProcedureCallErrorMessage` (StandaloneMessage 2) + `RpcFaultException` |
| `DefaultMessageConverter.ToArray` 복사 | `MessageSerializer.Deserialize(ReadOnlySpan)` 직통 |
| void도 항상 Request/Response | `[RemoteProcedure(..., OneWay = true)]` + `SendRPC` |
| MethodId 선언 순서만 | 명시 `methodId` (미지정 시 DRPCGEN004 warning, 중복 DRPCGEN005) |
| sync Outgoing | `[Obsolete]` + `{Method}Async` 권장; Sandbox는 Async 사용 |
| `ConnectAsync` CT 미전달 | 생성기에서 `RUDPConnector.ConnectAsync(..., cancellationToken)` 전달 |

## 잔여

| 이슈 | 영향 |
|------|------|
| Communication `MessageHandler`가 동기 `Action<object>` | 진짜 awaitable 수신 큐는 형제 프로젝트 변경 필요; DRPC는 fire-and-forget으로 큐 블로킹만 제거 |
| Incoming `partial` Implementation이 sync only | I/O가 긴 Implementation은 여전히 요청 Task를 점유 |
| Nested `[NonIdMessage]` + outer Standalone 이중 직렬화 | MessageProtocol 제약; ArrayPool 전면 도입 없음 |
| 응답 `SendAsync` ReliableType 미매핑 | 응답은 세션 기본(ReliableOrdered) |
| `ServerHub`/`ClientHub` 명명·`Netwrok` 오타 | 공개 API; 수정 시 breaking |
| Client/Server thin wrapper·`Test/` 없음 | 회귀는 Sandbox/수동 검증 |
| Hub `IDisposable` 전체 수명 모델 | disconnect cancel은 있으나 Hub/Listener Dispose API는 없음 |

## 권장 사용

- Outgoing은 `{Method}Async`만 사용한다 (sync는 Obsolete).
- 계약에 **명시 MethodId**를 넣는다.
- 알림성 void는 `OneWay = true`.
- 필요 시 `hub.RpcTimeout`을 조정하거나 `Timeout.InfiniteTimeSpan`으로 끈다.

## 관련

- [[FAQ]]
- [[Data-Flow]]
- [[Components]]
- [[Overview]]
- [[Public-API]]
- [[Configuration]]
