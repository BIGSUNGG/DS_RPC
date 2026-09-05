# DS_RPC (DRPC)

RUDP 위에서 동작하는 .NET 분산 RPC 라이브러리. **전송은 [DS_Communication](https://github.com/BIGSUNGG/DS_Communication)(RUDP),
직렬화는 [DS_MessageProtocol](https://github.com/BIGSUNGG/DS_MessageProtocol)** 에 위임하고, DRPC 는 계약·생성·런타임만 담당한다.

사용자는 내부(패킷·CallId·직렬화)를 모른 채 인터페이스에 특성만 붙여 RPC 를 선언·호출한다.

## 사용법

```csharp
using DRPC;
using DRPC.Shared.Interface;

public interface IGameServerProcedures : IServerProcedureDeclarations
{
    [RemoteProcedure(methodId: 0)]                              // 기본 ReliableOrdered
    int Add(int value1, int value2);

    [RemoteProcedure(RpcDeliveryMode.Sequenced, 2)]             // 호출 방식 교체
    void SetPosition(int playerId, float x, float y);

    [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 3, OneWay = true)]
    void LogChat(string text);                                  // 응답 없음
}
```

서버는 허브에서 받을 메서드의 `_Implementation` partial 만 채운다. 클라이언트는 접속 후 생성된 `*Async` 를 await 한다.

```csharp
// 서버
await using var handle = await GameServerHub.ListenAsync(9050, "sandbox-key", async hub =>
{
    float sum = await hub.EchoSumAsync(new List<float> { 1.5f, 2.25f });   // 서버 → 클라이언트 역호출
});

// 클라이언트
using var hub = await GameClientHub.ConnectAsync("127.0.0.1", 9050, "sandbox-key");
int result = await hub.AddAsync(2, 3);
```

생성기(`DRPC.CodeGenerator` 를 analyzer 로 참조)가 `{Method}Async` 스텁·수신 디스패치·접속/리스닝·페이로드 인코딩을 만든다.
양방향 호출, OneWay, 메서드별 전송 방식(`Unreliable`·`ReliableUnordered`·`Sequenced`·`ReliableOrdered`·`ReliableSequenced`),
DTO(`[NonIdMessage]`)와 그룹 다형성를 지원한다.

## 패키지

| 패키지 | 내용 |
| -------- | ------ |
| `DRPC.Attribute` | `[RemoteProcedure]`, `RpcDeliveryMode` (의존성 없음) |
| `DRPC.Shared` | `HubBase` 런타임, 와이어 메시지, 오류 모델 |
| `DRPC.Client` / `DRPC.Server` | 측별 허브 베이스·접속/리스닝 |
| `DRPC.CodeGenerator` | Roslyn source generator (DevelopmentDependency) |

모두 `netstandard2.1`(생성기는 netstandard2.0) — Unity 포함 .NET 프레임워크에서 사용 가능하다.
의존 버전은 `Directory.Build.props` 의 `MessageProtocolPackageVersion` · `CommunicationPackageVersion` 이 단일 사실원이다.

## 빌드·예제·테스트

```powershell
dotnet build DRPC.slnx -c Release
dotnet test  DRPC.slnx -c Release     # 단위 19 · 생성기 22 · RUDP 루프백 E2E 12
dotnet run --no-build -c Release --project Sandbox/Sandbox.Server
dotnet run --no-build -c Release --project Sandbox/Sandbox.Client
```

문서는 [`Document/`](Document/) Obsidian vault(사람용 진입점: [`Document/01-Overview/Home.md`](Document/01-Overview/Home.md)).

## 아카이브

재구축 이전 1.x 코드와 문서는 [`Legacy/`](Legacy/)에 있다.

| 경로 | 설명 |
| ------ | ------ |
| `Legacy/DRPC.slnx` | 기존 솔루션 |
| `Legacy/Source` | 구 DRPC.Attribute · Shared · Client · Server · CodeGenerator |
| `Legacy/Sandbox` | 구 예제(Contracts · Server · Client) |
| `Legacy/Test` | 구 DRPC.Shared.Tests |
| `Legacy/Document` | 구 Obsidian vault |
