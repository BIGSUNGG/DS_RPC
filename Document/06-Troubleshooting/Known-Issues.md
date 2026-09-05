---
project: DS_RPC
type: troubleshoot
status: stable
tags: [troubleshooting, known-issues, build]
updated: 2026-09-05
---

# Known-Issues — 재구축 2.0.0

알려 한계·함정·재현 안 되는 것. 설계 근거는 [[../05-Decisions/0002-async-only-delivery-and-payload|ADR-0002]].

## 사용자 함정

### `[RemoteProcedure(0)]` 은 methodId 가 아니라 Unreliable

`RemoteProcedure` 의 첫 positional 인자는 `RpcDeliveryMode mode` 다. `0` 은 열거형 첫 값 `Unreliable` 로 해석된다.
methodId 만 지정하려면 **명명 인자**를 쓴다: `[RemoteProcedure(methodId: 3)]`.
생성기가 DRPCGEN004 로 명시 methodId 를 권고하는 이유도 이 모호성 때문이다.

### 계약 메서드에서 `Task`/`Task<T>` 를 쓰면 DRPCGEN003

계약은 plain 반환 타입(`int Add(int a, int b)`)으로 선언한다. 호출측 표면은 `{Method}Async`(반환 `Task<T>`),
구현측은 `{Method}_Implementation`(반환 `Task<T>`)으로 생성기가 만든다.

### 오버로드는 지원 안 됨

같은 계약 인터페이스 안에서 메서드명이 겹치면 DRPCGEN003. 수신 디스패치가 메서드명 기반으로 생성되기 때문이다.
이름을 다르게 쓰거나 별도 계약 인터페이스로 나눈다.

### 매개변수 타입 지경

지원: 프리미티브·`string`(null 포함)·`enum`·`T?`(프리미티브/enum)·`byte[]`·1차원 배열·`List<T>`·MessageProtocol 메시지 타입.
미지원: `ref/out`·제네릭 메서드·`Dictionary`/`IEnumerable<T>`(List 이외)·`Span`·`Span` 계열.
`byte[]`·배열·`List<T>` 는 **non-null** 이어야 한다(null 이면 NRE). DTO 안의 필드는 MessageProtocol 규칙을 그대로 따른다.

### `MaxConcurrentIncoming` 은 처리 중에 바꾸지 않는다

실행 중 변경하면 기존 세마포어가 풀려나가 상한이 지켜지지 않는다. 연결 직후·유휴 시에만 설정(허브 XML 문서에도 명시).

## 동작상 알려진 한계

- **`RpcErrorCode.Timeout`/`Disconnected` 는 와이어로 전송되지 않는다.** 타임아웃은 호출 측 `TimeoutException`,
  끊김은 호출 측 `InvalidOperationException` 으로만 관찰된다(코드는 계약 안정성을 위해 정의만 유지).
- **응답 지연 시 만료 스캔은 1초 단위**다(허브 공용 타이머 1개). `RpcTimeout` 을 1초 미만으로 놓으면 실제 만료가 최대 1초 늦어진다.
- **one-way 수신 실패는 조용하다.** one-way 은 대기표가 없어 전송 실패·처리 예외가 호출측에 되돌아오지 않는다
  (처리 예외는 `Trace` 로만 남는다). 전달이 필요하면 one-way 를 쓰지 말 것.
- **페이로드 와이어 포맷은 버전 간 호환을 보장하지 않는다.** DRPC 고유 flat 인코딩(ADR-0002 결정 4). 양쪽을 같은 버전으로 빌드한다.

## 빌드·환경

### CLI 로 `Debug` 빌드 시 생성기 DLL 복사 실패 (MSB3021/MSB3027)

Roslyn 언어 서버(`CSharpLanguageServer`)가 `Source/DRPC.CodeGenerator/bin/Debug/netstandard2.0/DRPC.CodeGenerator.dll`
을 analyzer 로 로드해 점유한다. 증상은 "다른 프로세스에 의해 사용 중".
**해결**: CLI 빌드·테스트는 `-c Release` 를 쓴다(`dotnet build DRPC.slnx -c Release`). 이미 IDE 가 연 경우 Debug 재빌드 전에 IDE 를 닫거나 Restart 한다.

### 언어 서버가 새 프로젝트에서 참조를 못 푸는 현상

csproj 를 방금 만든 직후에는 restore 자산이 없어 LSP 진단이 "`Xunit`/`Microsoft.CodeAnalysis` 를 찾을 수 없음" 류의
**가짜 오류**를 낸다. `dotnet restore DRPC.slnx` 후 실제 빌드/테스트 결과을 기준으로 판단한다.

### 생성 산출물 보기

```powershell
dotnet build Sandbox/Sandbox.Server/Sandbox.Server.csproj -c Release `
  -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=obj/genR
# obj/genR/DRPC.CodeGenerator/DRPC.CodeGenerator.RpcIncrementalGenerator/GameServerHub.g.cs
```

## 회귀 방지를 위해 남아 있는 테스트

| 위험 | 테스트 |
| ------ | -------- |
| DRPC↔RUDP 열거형 대응 어긋남 | `DRPC.Shared.Tests.HubBaseTests.RpcDeliveryMap_CoversEveryMode` |
| one-way 신호(CallId 0) 후퇴 | `…Incoming_OneWay_IsNotAnswered`, E2E `OneWay_call_reaches_peer_without_response` |
| 지연/중복 응답이 다른 호출을 오염 | `UnexpectedResponseOrError_IsIgnoredWithoutFaultingOthers` |
| NonId 와 그룹 다형성의 직렬화 경로 뒤바뀜 | 생성기 `NonId_message_argument_uses_typed_serializer`, `Group_root_argument_keeps_runtime_type_over_the_wire`, E2E `Group_message_keeps_runtime_type_over_the_wire` |
| sync 스텁 부활(회귀) | 생성기 `Outgoing_generates_async_only_stub`(`[global::System.Obsolete` 금지) |

## 범위 밖 (이번 재구축에서 미구현)

- F8 NuGet 게시 파이프라인(`v*` 태그 → pack/publish 워크플로) — 라이브러리는 pack 가능한 상태지만 게시 CI 는 없다.
- F10 `TemplateSource/` 스캐폴드 템플릿.
- 페이로드 이중 직렬화·버퍼 풀링 최적화(측정 후 결정, ADR-0001 Legacy 유지).

## 관련

- [[../01-Overview/Feature-Spec|Feature-Spec]] · [[../03-Reference/Public-API|Public-API]] · [[../04-Guides/Getting-Started|Getting-Started]]
