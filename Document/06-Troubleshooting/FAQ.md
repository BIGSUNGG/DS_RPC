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

A: Hub를 `partial`로 선언했는지(DRPCGEN001), `ServerHub`/`ClientHub`를 상속했는지(DRPCGEN002), 파라미터·반환 타입이 지원되는지(DRPCGEN003)를 확인한다. [[Public-API]] 제약 절 참고.

## Q: 단위 테스트 프로젝트는 어디에 있나?

A: 현재 저장소에 `Test/` 폴더는 없다. 동작 확인은 `Examples/Sandbox.*`를 사용한다.

## Q: RPC 응답이 안 오면 타임아웃되나?

A: `HubBase.RequestRPC`는 응답 TCS가 완료될 때까지 `await`한다. Hub 수준의 타임아웃·재시도 설정은 없다. CallId 불일치 시 `InvalidOperationException`이 난다. [[Data-Flow]] 참고.

## 관련

- [[How-To]]
- [[Getting-Started]]
- [[Data-Flow]]
