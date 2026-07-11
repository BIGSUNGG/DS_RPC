---
project: DS_RPC
type: guide
status: draft
tags: [guide, how-to]
updated: 2026-07-11
---

# How-To

자주 하는 작업 레시피.

## 목록

- [[#계약 인터페이스 추가]]
- [[#Hub partial 구현]]

## 계약 인터페이스 추가

1. 공유 프로젝트(또는 Contracts)에 `IServerProcedureDeclarations` / `IClientProcedureDeclarations`를 상속한 인터페이스를 만든다.
2. 원격 메서드에 `[RemoteProcedure(ReliableType...., methodId)]`를 붙인다. 알림성 void는 `OneWay = true`.
3. 파라미터·반환 타입이 생성기 지원 범위인지 확인한다 ([[Public-API]]).

Sandbox 참고: `Sandbox/Sandbox.Contracts/PlaygroundProcedures.cs`.

## Hub partial 구현

1. 클라: `partial class ... : ServerHub<TServerDecls, TClientDecls>`
2. 서버: `partial class ... : ClientHub<TServerDecls, TClientDecls>` (`using DRPC.Server.Netwrok`)
3. Analyzer로 `DRPC.CodeGenerator`를 참조한다.
4. Incoming 메서드마다 생성되는 `{Name}_Implementation(...)`을 `partial`로 구현한다.
5. 연결: 클라는 `ConnectAsync`, 서버는 `ListenAsync` (생성 정적 메서드).

Sandbox 참고: `PlaygroundServerHub.cs`, `PlaygroundClientHub.cs`.

## 관련

- [[Getting-Started]]
- [[FAQ]]
- [[Public-API]]
- [[Data-Flow]]
