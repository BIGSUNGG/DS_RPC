---
project: DS_RPC
type: reference
status: stable
tags: [reference, api, packages, nuget]
updated: 2026-09-05
---

# Public-API — 재구축 2.0.0

사용자가 실제로 만지는 표면만 싣는다. 생성 산출물(`{Hub}.g.cs`)의 멤버는 §생성기가 만드는 것 참고.

## NuGet 의존 (고정 버전)

`Directory.Build.props` 의 두 프로퍼티가 단일 사실원이다.

| 프로퍼티 | 값 | 패키지 |
| --------- | ----- | -------- |
| `MessageProtocolPackageVersion` | `2.0.0` | `MessageProtocol`(런타임 + analyzers/dotnet/cs 생성기 포함) |
| `CommunicationPackageVersion` | `2.0.0` | `Communication.Shared`, `Communication.Network.RUDP.{Shared,Client,Server}` |

저장소 자체는 어떤 형제 프로젝트 경로도 참조하지 않는다(`Source/Sandbox/Test`의 csproj에서 `ProjectReference` 가
`../../DS_…` 로 가는 경우 없음 — 계약 확인 항목).

## DRPC.Attribute

```csharp
namespace DRPC;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RemoteProcedure : Attribute
{
    public RemoteProcedure(RpcDeliveryMode mode = RpcDeliveryMode.ReliableOrdered, int methodId = -1);
    public RpcDeliveryMode Mode { get; }
    public int MethodId { get; }
    public bool OneWay { get; set; }      // named arg: OneWay = true
}

public enum RpcDeliveryMode
{
    Unreliable, ReliableUnordered, Sequenced, ReliableOrdered, ReliableSequenced,
}
```

`[RemoteProcedure]` 인자 없이 붙이면 ReliableOrdered·MethodId 는 선언 순서(DRPCGEN004 경고).
**주의**: 첫 positional 인자는 `mode` 다. `[RemoteProcedure(0)]` 은 methodId 0 이 아니라 `Unreliable` —
methodId 만 지정할 때는 `[RemoteProcedure(methodId: 3)]` 을 쓴다(ADR-0002 결정 2).

## 계약 인터페이스 (DRPC.Shared)

```csharp
namespace DRPC.Shared.Interface;

public interface IServerProcedureDeclarations { }   // 서버가 구현, 클라이언트가 호출
public interface IClientProcedureDeclarations { }   // 클라이언트가 구현, 서버가 호출
```

메서드는 **plain 반환 타입**으로 선언한다(`int Add(int a, int b);`) — 생성된 스텁이 이미 async.

## 허브 베이스

```csharp
namespace DRPC.Client.Network;
public abstract class ClientHub<TSPD, TCPD> : HubBase<TSPD, TCPD>
    where TSPD : IServerProcedureDeclarations where TCPD : IClientProcedureDeclarations
{
    protected ClientHub(Func<HubBase, ISession> sessionFactory);
}

namespace DRPC.Server.Network;
public abstract class ServerHub<TSPD, TCPD> : HubBase<TSPD, TCPD> { /* 위와 동일 형태 */ }
```

`HubBase`(공용 런타임, 사용자 코드는 보통 상속만 한다):

| 멤버 | 의미 |
| ------ | ------ |
| `TimeSpan RpcTimeout { get; set; }` | 기본 30초. `Timeout.InfiniteTimeSpan`·0 이하는 무제한. 만료는 `TimeoutException` |
| `int MaxConcurrentIncoming { get; set; }` | 기본 0(무제한). 초과 시 non-one-way 는 `Overloaded` 오류, one-way 은 drop. **연결 직후·유휴 시에만 설정** |
| `event Action? Disconnected` | 끊김 1회(대기 호출은 이미 실패 처리된 뒤) |
| `void Disconnect()` | 대기 취소 + 세션 끊김 + 이벤트 |
| `void Dispose()` | `Disconnect()` + 타이머·세마포어 정리 |

## 생성기가 만드는 것 (허브마다)

```csharp
public partial class GameClientHub : ClientHub<IGameServerProcedures, IGameClientProcedures>
{
    // 접속 (클라이언트 측)
    public static Task<GameClientHub> ConnectAsync(string host, int port, CancellationToken ct = default);
    public static Task<GameClientHub> ConnectAsync(string host, int port, string? connectionKey, CancellationToken ct = default);

    // 리스닝 (서버 측) — ListenAsync(port, onConnected, ct) / (port, connectionKey, onConnected, ct) / (port, ct)
    public static Task<RpcListenHandle> ListenAsync(int port, string? connectionKey, Func<GameServerHub, Task> onConnected, CancellationToken ct = default);

    // Outgoing 스텁 (Async 전용 — sync 스텁은 없다)
    public Task<int> AddAsync(int value1, int value2);
    public Task NoteAsync(string text);                       // OneWay

    // Incoming: 사용자가 이 partial 을 구현한다
    private partial Task<int> EchoSum_Implementation(List<float> values);
}
```

`RpcListenHandle` : `IAsyncDisposable`/`IDisposable`, `Task? ListenTask` (중지·취소 시 반드시 완료).

## 헬퍼 (보일러플레이트 대체, 직접 호출할 일은 거의 없음)

| 타입 | 멤버 |
| ------ | ------ |
| `DRPC.Client.Network.RpcClient` | `Task<THub> ConnectAsync<THub>(string host, int port, string? connectionKey, Func<IMessageChannel, THub> hubFactory, CancellationToken ct = default)` |
| `DRPC.Server.Network.RpcHost` | `Task<RpcListenHandle> ListenAsync<THub>(int port, string? connectionKey, Func<IMessageChannel, THub> hubFactory, Func<THub, Task>? onConnected, CancellationToken ct = default)` |
| `DRPC.Shared.Network.HubSessionFactory` | `IMessageConverter Converter`, `ISession CreateRudpSession(IMessageChannel, IHubBase)`, `RudpTransportOptions CreateTransportOptions(string?)` |
| `DRPC.Shared.Network.RpcDeliveryMap` | `RudpSendOptions ToSendOptions(this RpcDeliveryMode)` — DRPC↔RUDP 열거형 유일한 대응 지점 |

## 오류 모델

와이어 오류는 `DRPC.Shared.RpcFaultException`(`CallId`, `ErrorCode`, 메시지=원문)으로 관찰된다.

`DRPC.Shared.Message.RpcErrorCode`:

| 상수 | 값 | 발생 지점 |
| ------ | ---- | ---------- |
| `Unhandled` | 1 | 피어 구현 본문 예외 (와이어) |
| `UnknownMethod` | 2 | 등록되지 않은 MethodId (와이어) |
| `Overloaded` | 5 | `MaxConcurrentIncoming` 초과 (와이어) |
| `Timeout` | 3 | 호출 측 `TimeoutException` — 와이어 코드로는 전송되지 않음 |
| `Disconnected` | 4 | 호출 측 `InvalidOperationException` — 와이어 코드로는 전송되지 않음 |

수신 디스패치: `DRPC.Shared.DRPCMessageHandler : MessageHandler`(3개 메시지 타입 라우팅 + 세션 끊김을 허브로 연결).

## 진단

| ID | 심각도 | 내용 |
| ---- | -------- | ------ |
| DRPCGEN001 | error | 허브 클래스가 `partial` 이 아님 |
| DRPCGEN002 | error | `ClientHub<,>`/`ServerHub<,>` 를 상속하지 않음(또는 형식 인자가 계약 아님) |
| DRPCGEN003 | error | 지원 안 되는 타입·`ref/out`·제네릭 메서드·`Task` 반환·중복 메서드명(오버로드) |
| DRPCGEN004 | warning | methodId 명시 없이 선언 순서 의존 |
| DRPCGEN005 | error | 한 계약 안에서 MethodId 중복 |
| DRPCGEN006 | error | `OneWay = true` 인데 반환이 void 가 아님 |

## 빌드·테스트

```powershell
dotnet build DRPC.slnx -c Release        # 5 라이브러리 + Sandbox 3 + Test 3
dotnet test  DRPC.slnx -c Release        # 53개 통과 (19 단위 / 22 생성기 / 12 RUDP 루프백)
```

`Debug` 로 CLI 빌드하면 Roslyn 언어 서버가 `DRPC.CodeGenerator.dll`(bin/Debug) 을 점유해 복사가 실패할 수 있다 —
CLI 는 `Release` 를 쓴다([[../06-Troubleshooting/Known-Issues|Known-Issues]]).

## 관련

- [[../02-Architecture/Overview|Architecture Overview]] · [[../04-Guides/Getting-Started|Getting-Started]] · [[../05-Decisions/0002-async-only-delivery-and-payload|ADR-0002]]
