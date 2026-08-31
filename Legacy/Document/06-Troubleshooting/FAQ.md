---
project: DS_RPC
type: troubleshoot
status: draft
tags: [faq]
updated: 2026-07-11
---

# FAQ

## Q: ServerHub와 ClientHub 중 어디에 상속해야 하나?

A: **클라이언트 앱**은 `ServerHub` 또는 별칭 `ClientToServerHub`. **서버 앱**은 `ClientHub` 또는 별칭 `ServerToClientHub` (`DRPC.Server.Netwrok`). [[0002-defer-netwrok-rename]]·[[GLOSSARY]] 참고.

## Q: `DRPC.Server.Netwrok` 네임스페이스가 오타인가?

A: 맞다. v2 전까지 유지([[0002-defer-netwrok-rename]]). `using`과 베이스 타입을 이 철자로 맞춘다.

## Q: Hub에서 생성 코드가 안 나온다 / DRPCGEN00x

A: DRPCGEN001 partial, DRPCGEN002 Hub base(alias 포함), DRPCGEN003 타입, DRPCGEN004 명시 MethodId 권장, DRPCGEN005 중복 MethodId, DRPCGEN006 OneWay+non-void. [[Public-API]] 참고.

## Q: 단위 테스트는 어디에 있나?

A: `Test/DRPC.Shared.Tests` (xUnit). HubBase CallId·timeout·error·concurrency·Disconnect를 커버한다. 통합 예제는 `Sandbox/`.

## Q: RPC 응답이 안 오면 타임아웃되나?

A: `HubBase.RpcTimeout`(기본 30초) 후 `TimeoutException`. Infinite면 무제한. 원격 예외는 `RpcFaultException`. 끊김은 `NotifyDisconnected`/`CancelPendingCalls`.

## Q: Incoming Implementation은 sync인가?

A: `partial Task` / `Task<T>` (async). Sandbox·TemplateSource도 Task 반환.

## Q: ListenAsync 반환 타입이 바뀌었나?

A: `Task<RpcListenHandle>`. `await using var handle = await Hub.ListenAsync(...)`. 리스너 루프 예외를 보려면 `handle.ListenTask`도 관측한다 ([[Known-Issues]]).

## Q: Sandbox.Contracts는 무엇을 참조하나?

A: `DRPC.Attribute`·`DRPC.Shared`·RUDP.Shared. 메시지 생성용 `MessageProtocol.CodeGenerator`는 Contracts에만 두고 **`buildtransitive` 없이** Client/Server로 흘리지 않는다 ([[Packages]]).

## Q: Sandbox 빌드 시 RpcIncrementalGenerator MissingMethodException / 생성 코드 없음?

A: SDK 10(Roslyn 5)에서 MessageProtocol 생성기 DLL이 이중 로드되거나 옆에 없으면 Nested 메시지 생성 단계에서 실패한다. (1) `DRPC.CodeGenerator`가 형제 MP CodeGenerator를 출력 옆에 두는지, (2) `Sandbox.Contracts`의 MP CodeGenerator에 **`buildtransitive`가 없는지** 확인. [[Packages]]·[[Configuration]].

## Q: 구조·성능상 남은 문제는?

A: 형제/major만 [[Known-Issues]] **잔여**. 이중 직렬화는 [[0001-defer-double-serialization]].

## 관련

- [[Known-Issues]]
- [[Structure-Performance]]
- [[Packages]]
- [[Configuration]]
- [[How-To]]
- [[Getting-Started]]
- [[Data-Flow]]
