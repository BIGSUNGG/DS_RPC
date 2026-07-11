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
| `ListenAsync` | `port`, `connectionKey` (선택, 기본 `""`), `onConnected`, `CancellationToken` | 서버 리스닝; peer마다 Hub 콜백 |

메서드별 전송 reliability는 `[RemoteProcedure(ReliableType, methodId)]`로 계약에 고정된다. one-way는 `OneWay = true`(void만).

| 키 | 기본 | 설명 |
|----|------|------|
| `HubBase.RpcTimeout` | `30s` | Outgoing `RequestRPC` 응답 대기. `Timeout.InfiniteTimeSpan` 또는 `<= Zero`면 무제한 |

Sandbox 예제 기본값: host `127.0.0.1`, port `9050`, key `sandbox-key`.

## 관련

- [[Packages]]
- [[Getting-Started]]
- [[Public-API]]
- [[Data-Flow]]
- [[Known-Issues]]
