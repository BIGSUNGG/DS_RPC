---
project: DS_RPC
type: reference
status: draft
tags: [api]
updated: 2026-07-11
---

# Public API

공개 타입·진입점 레퍼런스. 생성기 출력은 빌드 시 소비자 Hub에 추가되는 표면이다.

## 진입점

| API | 네임스페이스 | 설명 |
|-----|--------------|------|
| `RemoteProcedure` | `DRPC.Attribute` | `RemoteProcedure(ReliableType type, int methodId = -1)`; `OneWay` named |
| `IServerProcedureDeclarations` | `DRPC.Shared.Interface` | 서버 구현 계약 마커 |
| `IClientProcedureDeclarations` | `DRPC.Shared.Interface` | 클라이언트 구현 계약 마커 |
| `IHubBase` | `DRPC.Shared.Interface` | Request/Response/Error 수신 + `CancelPendingCalls` |
| `HubBase` / `HubBase<TSPD,TCPD>` | `DRPC.Shared.Network` | CallId·MethodCallActions·`RequestRPC`/`SendRPC`·`RpcTimeout` |
| `ProcedureCallRequestMessage` | `DRPC.Shared.Message` | `[StandaloneMessage(0)]` 요청 |
| `ProcedureCallResponseMessage` | `DRPC.Shared.Message` | `[StandaloneMessage(1)]` 응답 |
| `ProcedureCallErrorMessage` | `DRPC.Shared.Message` | `[StandaloneMessage(2)]` 오류 |
| `RpcErrorCode` | `DRPC.Shared.Message` | Unhandled / UnknownMethod / Timeout / Disconnected |
| `RpcFaultException` | `DRPC.Shared` | 원격 오류 응답을 호출측에 전달 |
| `DRPCMessageHandler` | `DRPC.Shared` | 메시지 라우팅 + disconnect 시 pending cancel |
| `ServerHub<T1,T2>` | `DRPC.Client.Network` | 클라 쪽 Hub 베이스 |
| `ServerSession` | `DRPC.Client.Network` | 클라 RUDP 세션 어댑터 |
| `ClientHub<T1,T2>` | `DRPC.Server.Netwrok` | 서버 쪽 Hub 베이스 (네임스페이스 철자 `Netwrok`) |
| `ClientSession` | `DRPC.Server.Netwrok` | 서버 RUDP 세션 어댑터 |

## 생성기 출력 (Hub partial)

| API | 적용 | 설명 |
|-----|------|------|
| `ConnectAsync` / `ListenAsync` | Hub 종류별 | CT를 커넥터/리스너에 전달 |
| `{Method}` | Outgoing | `[Obsolete]` sync; 내부 GetAwaiter |
| `{Method}Async` | Outgoing | 권장; one-way면 `SendRPC`, 아니면 `RequestRPC` |
| `{Method}_Implementation` | Incoming | 사용자 `partial` (sync) |
| nested 메시지 타입 | 생성 | `[NonIdMessage]` 파라미터/반환 래퍼 |

## 제약 (생성기)

- DRPCGEN001: Hub는 `partial`
- DRPCGEN002: `ServerHub<,>` 또는 `ClientHub<,>` (`Netwrok` 철자)
- DRPCGEN003: 지원 타입만 (void, 원시형, string, enumerable, MessageProtocol 메시지)
- DRPCGEN004: 명시 `methodId` 미지정 시 warning (선언 순서 fallback)
- DRPCGEN005: 계약 내 MethodId 중복 → error
- DRPCGEN006: `OneWay=true`는 void만

## 관련

- [[Packages]]
- [[Components]]
- [[Data-Flow]]
- [[Getting-Started]]
- [[Known-Issues]]
