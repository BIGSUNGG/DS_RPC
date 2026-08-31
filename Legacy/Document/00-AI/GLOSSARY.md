---
project: DS_RPC
type: context
status: draft
tags: [ai, glossary]
updated: 2026-07-11
---

# Glossary

도메인 용어. 새 용어는 여기 먼저 추가한다.

| 용어 | 설명 |
|------|------|
| DRPC | 이 저장소의 분산 RPC 스택 브랜드명 |
| RemoteProcedure | 원격 호출 Attribute (`ReliableType`, 명시 `methodId`, optional `OneWay`) |
| Shared | Client/Server가 공유하는 Hub·메시지·핸들러 |
| CodeGenerator | RPC·계약 코드를 생성하는 Roslyn 소스 생성기 |
| ServerDecls / ClientDecls | `IServerProcedureDeclarations` / `IClientProcedureDeclarations` 구현 계약 |
| ServerHub | 클라이언트 쪽 Hub 베이스 (`DRPC.Client.Network`) — 서버에 연결 |
| ClientHub | 서버 쪽 Hub 베이스 (`DRPC.Server.Netwrok`) — 클라이언트 peer용 |
| MethodId | `[RemoteProcedure]` 명시 ID(권장); 미지정 시 선언 순서 fallback |
| CallId | 요청-응답 짝을 맞추는 uint; 완료 후 재사용 |
| OneWay | 응답 없는 void RPC (`SendRPC`) |
| HubBase | CallId·MethodCallActions·RequestRPC/SendRPC·RpcTimeout 런타임 |
| ProcedureCallRequest/Response/Error | StandaloneMessage 0/1/2 RPC 와이어 메시지 |
| RpcFaultException | 원격 Error 메시지를 호출측에 전달하는 예외 |
| ReliableType | Communication RUDP의 전송 신뢰도; Attribute로 메서드에 지정 |
| RUDP | DS_Communication 기반 Reliable UDP 전송 계층 |
| `_Implementation` | Incoming RPC를 사용자가 구현하는 생성 `partial` 메서드 |

## 공통 (DS 스택)

| 용어 | 설명 |
|------|------|
| netstandard2.1 | Unity 및 다중 .NET 런타임 호환 타깃 (라이브러리) |
| NuGet | 패키지 배포 단위 |
| Sandbox | `Sandbox/Sandbox.*` 샘플·데모 |
| MessageProtocol | 형제 프로젝트의 메시지 직렬화 |
| Communication | 형제 프로젝트의 네트워크 전송 |

## 관련

- [[CONTEXT]]
- [[CONVENTIONS]]
- [[Public-API]]
