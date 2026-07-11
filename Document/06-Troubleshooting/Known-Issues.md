---
project: DS_RPC
type: troubleshoot
status: draft
tags: [troubleshoot, performance, architecture]
updated: 2026-07-11
---

# Known Issues — 구조·성능·병목

코드 기준(`HubBase`, 생성기 emitter, Client/Server 세션)으로 확인된 한계. 수정 전까지 설계·운영 시 전제로 둔다.

## 심각 — 동시성·스레드·누수

### 수신 경로 sync-over-async

`HubBase.OnReceiveRPCRequestMessage`가 핸들러 실행 후 `_session.SendAsync(...).GetAwaiter().GetResult()`로 블로킹한다. 생성 Outgoing sync API(`{Method}`)도 `RequestRPC`에 대해 동일하다.

- 네트워크/스레드풀 스레드가 송신·응답 대기에 묶이면 **지연·스레드 고갈·데드락** 위험이 있다.
- Incoming은 `Func<byte[], byte[]>`만 등록 가능해, Implementation이 I/O를 하면 같은 수신 경로를 막는다.

### CallId 할당 레이스

`RequestRPC`에서 `_notUsedMinCallId++`가 `Interlocked` 없이 증가한다. `ConcurrentStack` / `ConcurrentDictionary`와 달리 카운터는 보호되지 않는다.

- 동시 Outgoing 호출 시 CallId 충돌 → 잘못된 TCS 완료 또는 `WaitResponseTasks` 불일치 가능.

### 타임아웃·끊김 시 pending Task 누수

`RequestRPC`는 응답 TCS가 완료될 때까지 `await`한다. Hub 수준 타임아웃·재시도는 없다([[Data-Flow]], [[Configuration]]).

- Unreliable/유실 시 `WaitResponseTasks` 항목이 **영구 잔류**.
- `ServerSession` / `ClientSession`의 `OnDisconnected`는 `Console.WriteLine`만 하고 pending TCS를 완료하지 않는다.
- Implementation 예외 시 호출자에게 에러 응답이 없어 **호출 측이 무한 대기**할 수 있다.

장시간 서버에서는 딕셔너리·CallId 재사용 스택 비대화로 메모리·조회 비용이 커진다.

## 성능 — 직렬화·할당

### 이중(삼중) 직렬화와 복사

호출당 대략:

1. 파라미터 → nested `[NonIdMessage]` → `byte[]` (`ParameterData`)
2. `ProcedureCallRequestMessage`로 다시 직렬화
3. 수신 시 생성 `DefaultMessageConverter.Deserialize`가 `ReadOnlySpan`을 `ToArray()`로 복사

`ArrayPool` / zero-copy 경로가 없어 **GC 압력이 처리량 상한**이 되기 쉽다.

### 항상 Request/Response

void Outgoing도 응답을 기다린다. one-way / fire-and-forget / streaming이 없어 고빈도 이벤트·알림에 RTT와 할당이 붙는다.

### 호출당 할당

`MessageSendContext`, 요청/응답 메시지, TCS, 여러 `byte[]`가 호출마다 생긴다. `RequestRPC`는 `await` 후 `Task.Result`를 다시 읽는다(불필요).

## 구조·계약

| 이슈 | 영향 |
|------|------|
| `ServerHub`(클라) / `ClientHub`(서버) 명명 | 진입점 혼동 ([[FAQ]], [[GLOSSARY]]) |
| `DRPC.Server.Netwrok` 오타 고정 | 공개 API·생성기 진단에 묶임; 수정 시 breaking |
| MethodId = 계약 인터페이스 선언 순서 | 중간 삽입/순서 변경만으로 와이어 계약 불일치 |
| Client/Server가 thin wrapper | 세션 수명·백프레셔가 Shared/생성기에 분산 |
| Hub `Dispose`/취소 미연동 | 리스너·pending RPC 수명 모델 없음 |
| `Test/` 없음 | 동시성·끊김·직렬화 회귀 검증 어려움 |
| 응답 `SendAsync`에 `ReliableType` 미지정 | 요청만 Attribute reliability; 응답은 세션 기본값 |
| `ConnectAsync`의 `CancellationToken` | Listen 쪽과 달리 커넥터 연결에 CT가 실질 반영되지 않는 생성 형태 |

## 완화 (현재 코드 기준)

- Outgoing은 가능하면 `{Method}Async`만 쓰고, sync API·연결 콜백 안에서의 sync RPC는 피한다.
- Unreliable RPC는 “응답이 안 올 수 있음”을 전제로 두고, 장수명 Hub에서 대량 호출을 피한다.
- 계약 인터페이스 메서드 **순서·삽입을 바꾸지 않는다** (MethodId 안정성).
- Implementation에서 예외가 나면 상대 Outgoing이 멈출 수 있으므로, 구현 측에서 예외를 삼키거나 로컬에서만 처리한다(와이어 에러 프로토콜 없음).

## 개선 우선순위 (제안)

1. 수신/송신 경로 async화; sync Outgoing은 폐기 또는 비권장
2. CallId `Interlocked`; disconnect 시 pending TCS `Cancel` / `SetException`
3. Hub/메서드 단위 타임아웃
4. 직렬화 경로 단일화(`Span`/`ArrayPool`, 불필요 `ToArray` 제거) + void one-way
5. MethodId 안정화(이름 해시 또는 명시 ID Attribute)
6. Hub 수명주기·에러 응답 프로토콜

## 관련

- [[FAQ]]
- [[Data-Flow]]
- [[Components]]
- [[Overview]]
- [[Public-API]]
- [[Configuration]]
