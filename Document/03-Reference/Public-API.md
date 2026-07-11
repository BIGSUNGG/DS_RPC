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
| `RemoteProcedure` | `DRPC.Attribute` | 메서드용 Attribute. 생성자 `RemoteProcedure(ReliableType type)` |
| `IServerProcedureDeclarations` | `DRPC.Shared.Interface` | 서버 구현 계약 마커 |
| `IClientProcedureDeclarations` | `DRPC.Shared.Interface` | 클라이언트 구현 계약 마커 |
| `IHubBase` | `DRPC.Shared.Interface` | Request/Response 수신 콜백 |
| `HubBase` / `HubBase<TSPD,TCPD>` | `DRPC.Shared.Network` | RPC CallId·MethodCallActions·RequestRPC 런타임 |
| `ProcedureCallRequestMessage` | `DRPC.Shared.Message` | `[StandaloneMessage(0)]` 요청 |
| `ProcedureCallResponseMessage` | `DRPC.Shared.Message` | `[StandaloneMessage(1)]` 응답 |
| `DRPCMessageHandler` | `DRPC.Shared` | Request/Response → Hub 라우팅 |
| `ServerHub<T1,T2>` | `DRPC.Client.Network` | 클라 쪽 Hub 베이스 |
| `ServerSession` | `DRPC.Client.Network` | 클라 RUDP 세션 어댑터 |
| `ClientHub<T1,T2>` | `DRPC.Server.Netwrok` | 서버 쪽 Hub 베이스 (네임스페이스 철자 `Netwrok`) |
| `ClientSession` | `DRPC.Server.Netwrok` | 서버 RUDP 세션 어댑터 |

## 생성기 출력 (Hub partial)

`partial` Hub가 `ServerHub<,>` 또는 `ClientHub<,>`를 상속할 때 생성된다.

| API | 적용 | 설명 |
|-----|------|------|
| `ConnectAsync(host, port, ct)` / `ConnectAsync(host, port, connectionKey, ct)` | `ServerHub` 계열 | RUDP 연결 후 Hub 인스턴스 반환 |
| `ListenAsync(port, onConnected, ct)` / `ListenAsync(port, connectionKey, onConnected, ct)` | `ClientHub` 계열 | 리스닝; peer 연결 시 Hub를 `onConnected`에 전달 |
| `{Method}` / `{Method}Async` | Outgoing 계약 | 파라미터 직렬화 후 `RequestRPC` |
| `{Method}_Implementation(...)` | Incoming 계약 | 사용자가 구현하는 `partial` 메서드 |
| nested 파라미터/반환 메시지 타입 | 생성 | MessageProtocol `[NonIdMessage]` 등으로 마샬 |

## 제약 (생성기)

- Hub 타입은 `partial`이어야 한다 (DRPCGEN001).
- 베이스는 `DRPC.Client.Network.ServerHub<,>` 또는 `DRPC.Server.Netwrok.ClientHub<,>` (DRPCGEN002).
- 지원 타입: void, 원시형, string, enumerable/array, MessageProtocol 메시지 Attribute가 붙은 타입. generic·ref/out 불가 (DRPCGEN003).

## 관련

- [[Packages]]
- [[Components]]
- [[Data-Flow]]
- [[Getting-Started]]
