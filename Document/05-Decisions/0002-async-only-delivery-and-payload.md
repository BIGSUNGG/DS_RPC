---
project: DS_RPC
type: adr
status: accepted
tags: [adr, generator, transport, payload]
updated: 2026-09-05
---

# 0002 — Async 전용 스텁, 자체 전송 열거형, CallId 0 one-way, flat 페이로드

## Status

Accepted (2026-09-05) — 재구축 구현과 함께 확정.

## Context

재구축 시작 시점의 `Source/`는 비어 있었고, 레거시(`Legacy/`)는 다음을 안고 있었다.

1. 계약 메서드마다 `[Obsolete]` sync 스텁 + `{Method}Async` 를 함께 생성(sync 은 `GetAwaiter().GetResult()` 블로킹).
2. `[RemoteProcedure(ReliableType …)]` 가 전송 스택의 열거형을 그대로 노출. 이 열거형은 DS_Communication 2.0.0 에서
   `RudpDeliveryMethod` 로 개칭·이동됨(레거시 명세는 구 이름에 고정).
3. one-way 판정을 수신 측 `OneWayMethodIds` 등록표로 수행.
4. 메서드별 래퍼 메시지 타입(`{Method}_Paramter`, `{Method}_Return`)을 생성하고, 그 직렬화 코드를 얻기 위해
   DRPC.CodeGenerator 가 MessageProtocol.CodeGenerator 를 **프로젝트 참조**(`DS_MessageProtocol` 하드 경로)했다.
   → 레거시 CI의 pack 실패 잔여 이슈.

동시에 형제 스택은 NuGet `2.0.0` 으로 이미 게시돼 있었다(`MessageProtocol`, `Communication.Network.RUDP.*`).
재구축은 이 두 패키지를 **최신 NuGet판으로만** 참조하는 것이 목표 범위였다.

## Decision

1. **Async 전용**: 생성기는 `{Method}Async` 만 만든다. sync 스텁은 생성하지 않는다.
2. **DRPC 자체 `RpcDeliveryMode`**: `DRPC.Attribute` 에 정의(런타임 의존 0). 계약은
   `[RemoteProcedure(RpcDeliveryMode mode = ReliableOrdered, int methodId = -1)]`.
   → `RudpDeliveryMethod` 대응은 `DRPC.Shared/Network/RpcDeliveryMap.cs` 의 **name-based switch 한 파일에만** 존재한다.
   값 캐스팅이 아니라 명시 대응이라 upstream 재번호 시 컴파일/테스트(`RpcDeliveryMap_CoversEveryMode`)로 잡힌다.
3. **one-way 신호는 `CallId == 0`**: `SendRPC` 는 0 고정, `RequestRPC` 는 1부터 할당하므로 수신 측은
   `message.CallId == 0` 으로만 판정한다. `OneWayMethodIds` 등록표는 폐기.
4. **flat 페이로드 + 래퍼 타입 폐기**: 매개변수와 반환 값은 메서드별 래퍼 메시지 없이
   `MessageBufferWriter` 에 선언 순서대로 이어 붙인다. 메시지 타입 값은 MessageProtocol 런타임에 위임한다.
   - `[NonIdMessage]`(또는 타입 고정 직렬화) → 생성된 정적 `MessageSerializer.Serialize<T>/Deserialize<T>`
   - ID 헤더를 가진 메시지(Standalone/Group/Generic) → `SerializeToWriter`/`DeserializeFromReader` (object dispatch, 그룹 다형성 유지)
   DRPC.CodeGenerator 는 `Microsoft.CodeAnalysis.CSharp` 만 참조하고 **MessageProtocol 생성기를 참조하지 않는다**.

## Consequences

- (1) 블로킹 데드락 리스크 제거, 생성 코드·스냅샷 테스트가 절반으로 줄어든다. 레거시 1.x 소비 코드와 소스 호환은 깨진다(§재구축 수용).
- (2) 사용자 계약면은 `DRPC` 하나만 보면 된다. 단 `[RemoteProcedure(0)]` 은 methodId 가 아니라 **mode 0(Unreliable)** 이라는
  함정이 있다 — methodId 를 지정할 때는 `methodId:` 명명 인자를 쓰거나 mode 와 함께 명시해야 한다(DRPCGEN004 가 경고로 유도).
- (3) 두 계약 집합(IServer/IClient)이 같은 MethodId 를 서로 다르게 one-way 로 선언해도 어긋나지 않고,
  미등록 MethodId 요청도 올바른 규칙으로 처리된다. 대기 응답이 없는 one-way 오류 응답도 발생하지 않는다.
- (4) 레거시 CI pack 실패의 원인(형제 저장소 하드 경로 프로젝트 참조)이 구조적으로 사라진다.
  대신 페이로드 인코딩이 DRPC 고유의 것이 된다(단방향 호환만 보장, 버전 간 와이어 호환은 미제공 — 신규 프로젝트이므로 수용).
  `Timeout`/`Disconnected` 오류 코드는 와이어가 아니라 호출 측 예외(`TimeoutException`, `InvalidOperationException`)로만 관찰된다.

## 관련

- [[../01-Overview/Feature-Spec|Feature-Spec]] (F1·F2·F3·F5 반영)
- [[0001-hub-naming-and-version-2|0001 — Hub 명명 정렬과 2.0.0 버전 출발]]
- [[../02-Architecture/Overview|Architecture Overview]] §페이로드 인코딩
