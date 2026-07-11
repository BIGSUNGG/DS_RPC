---
project: DS_RPC
type: architecture
status: draft
tags: [architecture, components]
updated: 2026-07-11
---

# Components

패키지/어셈블리 단위 컴포넌트 맵.

| 패키지 | 설명 |
|--------|------|
| **DRPC.Attribute** | RPC 계약용 Attribute |
| **DRPC.Shared** | 공유 타입·직렬화·MessageProtocol 연동 |
| **DRPC.Client** | 클라이언트 RPC 및 RUDP 클라이언트 연동 |
| **DRPC.Server** | 서버 RPC 및 RUDP 서버 연동 |
| **DRPC.CodeGenerator** | Roslyn 분석기/소스 생성기 |

## 상세

| 컴포넌트 | 책임 | 주요 타입 | 의존 |
|----------|------|-----------|------|
| **DRPC.Attribute** | 원격 메서드 계약 표시 | `RemoteProcedure` (`DRPC.Attribute`) | `Communication.Network.RUDP.Shared` (`ReliableType`) |
| **DRPC.Shared** | Hub 런타임, RPC 메시지, 수신 핸들러, 계약 마커 | `HubBase` / `HubBase<TSPD,TCPD>`, `IHubBase`, `IServerProcedureDeclarations`, `IClientProcedureDeclarations`, `ProcedureCallRequestMessage` (StandaloneMessage 0), `ProcedureCallResponseMessage` (StandaloneMessage 1), `DRPCMessageHandler` | Attribute, MessageProtocol.Core, Communication.Shared / RUDP.Shared |
| **DRPC.Client** | 클라 쪽 Hub·세션 | `ServerHub<T1,T2>`, `ServerSession` (`DRPC.Client.Network`) | Shared, Communication RUDP.Client, LiteNetLib |
| **DRPC.Server** | 서버 쪽 Hub·세션 | `ClientHub<T1,T2>`, `ClientSession` (`DRPC.Server.Netwrok`) | Shared, Communication RUDP.Server, LiteNetLib |
| **DRPC.CodeGenerator** | Hub stub·마샬·연결 코드 생성 | `RpcIncrementalGenerator` → `RpcHubSourceGenerator` → `RpcHubEmitter`, `RpcMarshal`, `DiagnosticDescriptors` (DRPCGEN001–003) | Microsoft.CodeAnalysis.CSharp, MessageProtocol.CodeGenerator |

## Hub 런타임 (`HubBase`)

- `RequestRPC(methodId, parameterData, reliableType)`: CallId 할당 → `ProcedureCallRequestMessage` 전송 → `TaskCompletionSource`로 응답 대기
- `OnReceiveRPCRequestMessage`: `MethodCallActions[methodId]` 실행 후 `ProcedureCallResponseMessage` 회신
- `OnReceiveRPCResponseMessage`: 대기 Task 완료, CallId를 재사용 스택에 반환
- `DRPCMessageHandler`: Request/Response 메시지 타입을 Hub 콜백으로 라우팅

## 생성기 파이프라인

1. `partial class` + `ServerHub<,>` 또는 `ClientHub<,>` 상속 탐지
2. 계약 인터페이스의 `[RemoteProcedure]` 메서드에 선언 순서대로 MethodId 부여
3. **Outgoing**: sync + `Async` 래퍼 → 직렬화 → `RequestRPC`
4. **Incoming**: 디스패치 + 사용자 `partial {Name}_Implementation(...)`
5. **Connection**: 클라 `ConnectAsync`, 서버 `ListenAsync`
6. 파라미터/반환용 nested 메시지 타입 + MessageProtocol 직렬화 소스

서버 Hub 기준 Outgoing=ClientDecls·Incoming=ServerDecls, 클라 Hub는 반대.

## 명명 주의

- **ServerHub** = 클라이언트 프로젝트에서 사용 (서버 세션)
- **ClientHub** = 서버 프로젝트에서 사용 (클라이언트 세션)
- 서버 네임스페이스는 코드상 `DRPC.Server.Netwrok` (오타 고정)

## 관련

- [[Overview]]
- [[Packages]]
- [[Data-Flow]]
- [[Public-API]]
