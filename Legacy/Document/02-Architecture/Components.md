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
| **DRPC.Attribute** | 원격 메서드 계약 표시 | `RemoteProcedure` (`methodId`, `OneWay`) | `Communication.Network.RUDP.Shared` (`ReliableType`) |
| **DRPC.Shared** | Hub 런타임, RPC 메시지, 수신 핸들러, 계약 마커 | `HubBase`, `IHubBase`, `ProcedureCallRequest/Response/ErrorMessage` (0/1/2), `RpcFaultException`, `DRPCMessageHandler` | Attribute, MessageProtocol.Core, Communication.Shared / RUDP.Shared |
| **DRPC.Client** | 클라 쪽 Hub·세션 | `ServerHub<T1,T2>`, `ServerSession` (`DRPC.Client.Network`) | Shared, Communication RUDP.Client, LiteNetLib |
| **DRPC.Server** | 서버 쪽 Hub·세션 | `ClientHub<T1,T2>`, `ClientSession` (`DRPC.Server.Netwrok`) | Shared, Communication RUDP.Server, LiteNetLib |
| **DRPC.CodeGenerator** | Hub stub·마샬·연결 코드 생성 | `RpcIncrementalGenerator` → `RpcHubEmitter`, `DiagnosticDescriptors` (DRPCGEN001–006) | Microsoft.CodeAnalysis.CSharp 4.14; 형제 `MessageProtocol.CodeGenerator` (ProjectReference, analyzer 옆 DLL) |

## Hub 런타임 (`HubBase`)

- `RequestRPC` / `SendRPC`(one-way CallId 0): CallId(`Interlocked`, 비재사용) → 전송; RequestRPC는 Hub Timer 타임아웃 스캔
- `MaxConcurrentIncoming`, `MethodReliableTypes`, `Disconnected`, `Disconnect`/`IDisposable`
- `OnReceiveRPCRequestMessage` → `ProcessRequestAsync` → Response/Error
- `NotifyDisconnected` / `CancelPendingCalls`: disconnect 시 pending 실패

## 생성기 파이프라인

1. `partial class` + `ServerHub`/`ClientToServerHub` / `ClientHub`/`ServerToClientHub` 탐지
2. `[RemoteProcedure]`에서 MethodId·OneWay·ReliableType 수집
3. **Outgoing**: Obsolete sync + Async → `RequestRPC` 또는 `SendRPC`
4. **Incoming**: `async Task<byte[]>` 디스패치 + `partial Task`/`Task<T>` Implementation
5. **Connection**: `ConnectAsync` / `ListenAsync` → `RpcListenHandle`
6. nested 메시지 타입 + MessageProtocol 직렬화 소스

서버 Hub 기준 Outgoing=ClientDecls·Incoming=ServerDecls, 클라 Hub는 반대.

## 명명 주의

- **ServerHub** = 클라이언트 프로젝트에서 사용 (서버 세션)
- **ClientHub** = 서버 프로젝트에서 사용 (클라이언트 세션)
- 서버 네임스페이스는 코드상 `DRPC.Server.Netwrok` (오타 고정)

## Sandbox (비배포)

| 프로젝트 | 역할 | Analyzer |
|----------|------|----------|
| `Sandbox.Contracts` | 계약·메시지. ProjectRef: Attribute·Shared. Package: RUDP.Shared + `MessageProtocol.CodeGenerator` (`PrivateAssets=all`, `IncludeAssets`에 **`buildtransitive` 없음**) | MP CodeGenerator만 |
| `Sandbox.Client` / `Sandbox.Server` | Hub 실행. Contracts + Client/Server ProjectRef | `DRPC.CodeGenerator` (MP analyzer는 Contracts에서 전파되지 않음) |

근거: `Sandbox/Sandbox.Contracts/Sandbox.Contracts.csproj`. [[Packages]]·[[Configuration]].

## 관련

- [[Overview]]
- [[Packages]]
- [[Data-Flow]]
- [[Structure-Performance]]
- [[Public-API]]
