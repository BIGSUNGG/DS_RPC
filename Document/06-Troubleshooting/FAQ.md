---
project: DS_RPC
type: troubleshoot
status: draft
tags: [faq]
updated: 2026-07-11
---

# FAQ

## Q: ServerHub와 ClientHub 중 어디에 상속해야 하나?

A: **클라이언트 앱**은 `DRPC.Client.Network.ServerHub<,>`(서버에 연결). **서버 앱**은 `DRPC.Server.Netwrok.ClientHub<,>`(클라이언트 peer용). 이름이 “상대 세션” 기준이라 헷갈리기 쉽다. [[Components]]·[[GLOSSARY]] 참고.

## Q: `DRPC.Server.Netwrok` 네임스페이스가 오타인가?

A: 맞다. 코드·생성기 진단(DRPCGEN002)에 `Netwrok`로 고정되어 있다. `using`과 베이스 타입을 이 철자로 맞춰야 한다.

## Q: Hub에서 생성 코드가 안 나온다 / DRPCGEN00x

A: DRPCGEN001 partial, DRPCGEN002 Hub base, DRPCGEN003 타입, DRPCGEN004 명시 MethodId 권장(warning), DRPCGEN005 중복 MethodId, DRPCGEN006 OneWay+non-void. [[Public-API]] 참고.

## Q: 단위 테스트 프로젝트는 어디에 있나?

A: 현재 저장소에 `Test/` 폴더는 없다. 동작 확인은 `Sandbox/Sandbox.*`를 사용한다.

## Q: RPC 응답이 안 오면 타임아웃되나?

A: `HubBase.RpcTimeout`(기본 30초) 후 `TimeoutException`. `Timeout.InfiniteTimeSpan`이면 무제한. 원격 예외는 `ProcedureCallErrorMessage` → `RpcFaultException`. 끊김 시 pending은 `CancelPendingCalls`로 실패한다. [[Data-Flow]]·[[Configuration]].

## Q: sync `{Method}`를 써도 되나?

A: 생성되지만 `[Obsolete]`이다. `{Method}Async`를 쓴다. 알림성 void는 `OneWay = true`.

## Q: 구조·성능상 남은 문제는?

A: [[Known-Issues]]의 **잔여** 절(이중 직렬화, 응답 ReliableType, Netwrok 명명, Test 부재 등).

## 관련

- [[Known-Issues]]
- [[How-To]]
- [[Getting-Started]]
- [[Data-Flow]]
