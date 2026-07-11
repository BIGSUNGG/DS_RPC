---
project: DS_RPC
type: reference
status: draft
tags: [configuration]
updated: 2026-07-11
---

# Configuration

빌드·런타임·패키징 설정.

## Directory.Build.props

### 루트 `Directory.Build.props`

| 키 | 역할 |
|----|------|
| `LangVersion` | `latest` |
| `IsPackable` | 기본 `false` (예제·앱) |
| `MessageProtocolPackageVersion` | MessageProtocol.* NuGet 버전 (현재 `1.0.0`) |
| `CommunicationPackageVersion` | Communication.* NuGet 버전 (현재 `1.0.0`) |
| PolySharp | netstandard에서 ModuleInitializer 등 지원 |

### `Source/Directory.Build.props`

| 키 | 역할 |
|----|------|
| 루트 props Import | Source 하위만 자동 Import될 때 루트 누락 방지 |
| `IsPackable` | `true` |
| `TargetFramework` | `netstandard2.1` |
| `Nullable` / `ImplicitUsings` | enable |
| `Version` | 기본 `1.1.0` |
| `Authors`, `RepositoryUrl`, `PackageReadmeFile` | 패키지 메타 |

## 런타임 옵션

Hub 생성 API(생성기 출력) 인자:

| API | 인자 | 설명 |
|-----|------|------|
| `ConnectAsync` | `host`, `port`, `connectionKey` (선택, 기본 `""`), `CancellationToken` | 클라 → 서버 RUDP 연결 |
| `ListenAsync` | `port`, `connectionKey` (선택, 기본 `""`), `onConnected`, `CancellationToken` | `Task<RpcListenHandle>`; peer마다 Hub 콜백. `await using`으로 Dispose |

메서드별 전송 reliability는 `[RemoteProcedure(ReliableType, methodId)]`로 계약에 고정된다. one-way는 `OneWay = true`(void만). 응답/에러도 Incoming MethodId의 ReliableType을 따른다.

| 키 | 기본 | 설명 |
|----|------|------|
| `HubBase.RpcTimeout` | `30s` | Outgoing `RequestRPC` 응답 대기. `Timeout.InfiniteTimeSpan` 또는 `<= Zero`면 무제한. Hub `Timer`가 1초 주기로 만료 스캔 |
| `HubBase.MaxConcurrentIncoming` | `0` (무제한) | `>0`이면 동시 Incoming 상한; 초과 시 `RpcErrorCode.Overloaded` (one-way는 drop) |
| `HubBase.Disconnected` | — | 끊김/`Disconnect` 시 이벤트 |

Sandbox 예제 기본값: host `127.0.0.1`, port `9050`, key `sandbox-key`.

## 빌드·Analyzer

| 항목 | 설정 |
|------|------|
| `DRPC.CodeGenerator` | 형제 `DS_MessageProtocol/.../MessageProtocol.CodeGenerator.csproj` ProjectReference; 빌드 후 `MessageProtocol.CodeGenerator.dll`을 출력 옆에 복사. `Microsoft.CodeAnalysis*.dll` / `System.*.dll`은 출력에서 삭제(호스트 Roslyn 사용) |
| `Sandbox.Contracts` | `MessageProtocol.CodeGenerator` NuGet: `PrivateAssets=all`, `IncludeAssets=runtime; build; native; contentfiles; analyzers` — **`buildtransitive` 제외**로 Client/Server에 MP analyzer가 흘러가지 않음 |
| Sandbox Client/Server | Analyzer로 `DRPC.CodeGenerator`만 참조 (Contracts의 MP 생성기와 이중 로드 방지) |

로컬에서 CodeGenerator를 쓰려면 형제 `DS_MessageProtocol` 저장소가 `../DS_MessageProtocol` 상대 경로에 있어야 한다.

## 관련

- [[Packages]]
- [[Getting-Started]]
- [[Public-API]]
- [[Data-Flow]]
- [[Known-Issues]]
