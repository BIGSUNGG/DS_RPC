---
project: DS_RPC
type: guide
status: stable
tags: [guide, quickstart, usage]
updated: 2026-09-05
---

# Getting-Started — RPC 선언하고 호출하기

목표: 전송·직렬화 코드를 한 줄도 쓰지 않고 RPC 를 왕복시킨다. 동작하는 전체 예제는
`Sandbox/`(Contracts·Server·Client)에 있다.

## 0. 준비

- .NET SDK 10 (`dotnet --version` ≥ 10.0.100). 라이브러리는 netstandard2.1 이라 Unity 에서도 참조 가능하다.
- NuGet: `MessageProtocol 2.0.0`, `Communication.Network.RUDP.* 2.0.0`(이 저장소는 `Directory.Build.props` 버전으로 고정).
- 앱 프로젝트는 다음을 참조한다:
  - 계약 프로젝트 → `DRPC.Attribute`, `DRPC.Shared`, `MessageProtocol`
  - 클라이언트/서버 앱 → `DRPC.Client`/`DRPC.Server` + analyzer 로 `DRPC.CodeGenerator`

```xml
<ProjectReference Include="..\..\Source\DRPC.CodeGenerator\DRPC.CodeGenerator.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

## 1. 계약 선언 (양쪽이 공유)

`IServerProcedureDeclarations` = 서버가 구현하고 클라이언트가 호출. `IClientProcedureDeclarations` = 반대.
반환 타입은 plain 하게(`Task` 없이) 쓴다 — 생성되는 스텁이 이미 async.

```csharp
using DRPC;
using DRPC.Shared.Interface;
using MessageProtocol;

public interface IGameServerProcedures : IServerProcedureDeclarations
{
    [RemoteProcedure(methodId: 0)]                              // 기본 ReliableOrdered
    int Add(int value1, int value2);

    [RemoteProcedure(RpcDeliveryMode.Sequenced, 2)]             // 전송 방식 오버라이드
    void SetPosition(int playerId, float x, float y);

    [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 3, OneWay = true)]
    void LogChat(string text);                                  // 응답 없음
}

[NonIdMessage]                                                  // DTO 직렬화는 MessageProtocol 이 만든다
public partial class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

## 2. 서버: 허브 + 구현 채우기

`ServerHub<서버계약, 클라이언트계약>` 을 partial 로 상속하고, 호출당하는 메서드마다
`{Name}_Implementation` partial 을 구현하면 끝이다(생성기가 정의 선언을 만들어 준다).

```csharp
using DRPC.Server.Network;

public partial class GameServerHub : ServerHub<IGameServerProcedures, IGameClientProcedures>
{
    private partial Task<int> Add_Implementation(int value1, int value2) => Task.FromResult(value1 + value2);

    private partial Task SetPosition_Implementation(int playerId, float x, float y) => Task.CompletedTask;

    private partial Task LogChat_Implementation(string text)
    {
        Console.WriteLine($"chat: {text}");
        return Task.CompletedTask;
    }
}
```

리스닝과 역호출(서버 → 클라이언트):

```csharp
await using var handle = await GameServerHub.ListenAsync(9050, "sandbox-key", async hub =>
{
    float sum = await hub.EchoSumAsync(new List<float> { 1.5f, 2.25f, 4f });   // 클라이언트 계약 스텁
});
```

## 3. 클라이언트: 접속하고 호출

```csharp
using var hub = await GameClientHub.ConnectAsync("127.0.0.1", 9050, "sandbox-key");

int result  = await hub.AddAsync(2, 3);
await hub.SetPositionAsync(7, 1.25f, -3.5f);
await hub.LogChatAsync("hello");
```

클라이언트도 자기 계약(`IGameClientProcedures`)의 `_Implementation` 은 같은 방식으로 채운다.

## 4. 호출 방식 바꾸기

| 요구 | 선언 |
| ------ | ------ |
| 기본(신뢰·순서) | `[RemoteProcedure(methodId: 0)]` |
| 유실 감수 최고 속도 | `[RemoteProcedure(RpcDeliveryMode.Unreliable, 1)]` |
| 최신 것만 도달 | `[RemoteProcedure(RpcDeliveryMode.ReliableSequenced, 2)]` |
| 응답 대기 없음 | `[RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 3, OneWay = true)]` |

`mode` 는 반드시 `RpcDeliveryMode` 값이다(첫 positional 인자가 methodId 아님).

## 5. 조정 가능한 런타임 설정

접속/리스닝 이후 허브 인스턴스에서:

```csharp
hub.RpcTimeout = TimeSpan.FromSeconds(5);      // 기본 30초. 무제한은 TimeSpan.Zero
hub.MaxConcurrentIncoming = 32;                // 기본 0(무제한). 연결 직후·유휴에만 설정
hub.Disconnected += () => Console.WriteLine("dropped");
hub.Dispose();                                 // 대기 호출 취소 + 세션 종료
```

## 6. 샌드박스 실행

```powershell
dotnet build DRPC.slnx -c Release
dotnet run --no-build -c Release --project Sandbox/Sandbox.Server   # 127.0.0.1:9050, key=sandbox-key
dotnet run --no-build -c Release --project Sandbox/Sandbox.Client
```

서버 콘솔에는 접속·역호출 결과·one-way 수신·그룹 다형성(`ShoutChatLine`) 수신까지 찍힌다.
서버는 ENTER 를 누르면 중지한다(stdin 이 닫히면 바로 종료).

## 관련

- [[../03-Reference/Public-API|Public-API]] — 표면 전체 · [[../02-Architecture/Overview|Architecture Overview]] — 내부 동작
- [[../06-Troubleshooting/Known-Issues|Known-Issues]]
