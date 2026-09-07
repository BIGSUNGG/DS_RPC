---
project: DS_RPC
type: meta
status: stable
tags: [meta, changelog]
updated: 2026-09-08
---

# Changelog

문서 변경 기록(최신 위). 코드 변경은 커밋 메시지로 추적한다.

## 2026-09-09 (7차)

- **형제 제안 노트 소비 + P3 채택 — `CreateRudpSession` 큐 옵션 통과** — DS_Communication `Proposals-Upstream.md`(신규) 확인: P1(TCP TLS·보류 질문으로 PENDING 등록), P2(CRC32c·v2.5.0 채택완료), P3(타임아웃 정책 통일 — ConnectTimeout 은 v2.2.0 완료, **FrameTimeout·MaxFrameLength 등 `MessageQueueOptions` 를 `CreateRudpSession` 선택 매개변수로 노출**), P4(운영 신호·후보 등록). E2E — 64바이트 상한 세션에서 소형 프레임 왕복 정상·초과 페이로드 송신 격리(2초 이내 실패)로 배관 실증. 테스트 103→104건.
- **형제 태그 확인 — 버전 유지 결정**: Communication 2.3.1(테스트·문서·제안노트), MessageProtocol 2.3.3(생성기 리팩터·golden 검증) 은 기능 차이 없음 → 패키지 버전 유지(2.3.0/2.3.2). FrameTimeout 기본값(30초·첫 바이트 후 마감 — 완전 유휴 미적용) 확인으로 잠재 유휴 단절 결함 부재도 확인.
- [[../03-Reference/Public-API|Public-API]] 팩토리 표, [[../00-AI/CONTEXT|CONTEXT]] 카운트, [[../00-AI/PENDING|PENDING]] P1/P4 등록.

## 2026-09-08 (6차)

- **`HubBase.AuthorizeRequestAsync` RPC 호출 권한 검증 훅** — 스펙 개선 영역 1(보안 「RPC 호출 권한 검증」) 처리. `protected virtual Task<bool>`(기본 전부 허용 — 하위호환), 서버 허브 override 로 메서드별 권한 검사(예: 관리자 전용 프로시저). 거부 시 non-one-way 는 신규 오류 코드 `RpcErrorCode.PermissionDenied`(6) 반환, one-way 는 폐기. **등록표 조회 전 판정** — 거부된 MethodId 의 존재 여부도 노출하지 않음. 단위 4건(기본 허용·거부 시 오류+미실행·one-way 조용한 폐기·훅 인자 검증).
- **형제 3차 채택 — Communication 2.3.0·MessageProtocol 2.3.2** — Communication: 파이프라인 검증 통일·**기본 공개 접속 키로 리슨 시작 시 경고**(DRPC null-키 리스너에 즉시 적용), MessageProtocol: 역직렬화 진입 와이어 헤더-MessageId 검증·컨텍스트 사전 사전크기 성능개선. 채택 중 NuGet 유입 지연(NU1102) 관측 — http-cache 클리어로 해소(약 1분 지연). 테스트 99→103건 통과.
- [[../03-Reference/Public-API|Public-API]] 오류 코드 표·HubBase 훅, [[../01-Overview/Feature-Spec|Feature-Spec]] F2·F3·상태, [[../00-AI/CONTEXT|CONTEXT]] 동기화.

## 2026-09-08 (5차)

- **형제 스택 2차 채택 — Communication 2.2.1·MessageProtocol 2.3.1** — Communication 2.1.0(TCP TLS — DRPC 미사용) 건너뙄고 2.2.0 **RUDP CRC32c 패킷 무결성 레이어**(옵트인, 프로토콜 처리 전 위반 패킷 폐기 — IPv4 UDP 체크섬 비활성 우회 방어) + 2.2.1 프레이밍 버퍼 압축 개선, MessageProtocol 2.3.1(PooledBuffer 이중 반납 수정·생성기 파싱 강화) 채택.
- **`RpcEndpointOptions` + `ConnectWithOptionsAsync`/`ListenWithOptionsAsync`** — 전송 옵션 일괄 지정 묶음 타입(`ConnectionKey`·`ConnectTimeoutMs`·`MaxConnections`·`EnableCrc32c`, `ToTransportOptions()`). 기존 매개변수 오버로드에 `string?` 와 같은 위치 null-리터럴 모호성(CS0121·런타임 미스바인딩)을 만들지 않도록 **별명 메서드**로 추가 — 기존 호출 전부 무영향. CRC32c 는 양단 일치 필수(와이어 비호환)·검출 전용 문서화.
- E2E — 양단 CRC32c 왕복 정상 + 미스매치(끄고 접속) 연결 수립 불가 확인, 단위 4건(매핑·기본값·음수 거부). 테스트 94→99건 통과.
- [[../03-Reference/Public-API|Public-API]] 헬퍼 표·신규 타입, [[../01-Overview/Feature-Spec|Feature-Spec]]·[[../00-AI/CONTEXT|CONTEXT]] 상태 동기화.
- **`v2.5.0` 릴리스** — 태그 푸시 → run 34152401923 success, 5개 패키지 flatcontainer HTTP 206 확인. 게시 상태·README 표기 갱신.

## 2026-09-08 (4차)

- **왕복 RPC 호출자 취소 지원** — 스펙 개선 영역 2(정확성 「호출 타임아웃·취소 누수」) 처리. `HubBase.RequestRPC` 가 선택 `CancellationToken` 수용(사전 취소 → 미송신·`OperationCanceledException`, 대기 중 취소 → 즉시 취소 완료·슬롯 반납·늦은 응답 무시, 송신된 요청 회수 안 함). 생성 스텁(일반·제네릭) 왕복 호출 전부 맨 끝 선택 토큰 매개변수 획득(매개변수 없는 스텁 포함, 무득수 앞 쉼표 없음), OneWay 는 대기 부재로 제외. 기존 호출 전부 소스 호환(선택 매개변수). 단위 — 사전 취소 미송신·취소 슬롯 반납(MaxPendingCalls 상한 1에서 재수용)·늦은 응답 무시, 생성기 — 스텁 서명 ct 핀 3건·one-way 무토큰 핀. 테스트 91→94건 통과.
- [[../03-Reference/Public-API|Public-API]] 스텁 예시, [[../01-Overview/Feature-Spec|Feature-Spec]] F2(타임아웃→타임아웃·취소)·F5 갱신.
- **`v2.4.0` 릴리스** — 태그 푸시 → run 34151310315 success, 5개 패키지 flatcontainer HTTP 206 확인. 게시 상태·README 표기 갱신.

## 2026-09-08 (3차)

- **`RpcHost.ListenAsync` `maxConnections` 오버로드** — 스펙 개선 영역 1(보안·리소스 고갈 「대량 호출·연결 고갈」) 처리. 동시 수락 연결 수 상한을 DRPC 리슨 경로에 노출(Communication 2.0.1 `RudpTransportOptions.MaxConnections` 채택 완결 — ConnectTimeout 에 이은 두 번째). 상한 도달 시 초과 접속은 즉시 거부되고 수락은 계속(연결 고갈 공격 방어), 0(기본)=무제한·동작 불변, 음수 거부. `HubSessionFactory.CreateTransportOptions` 세 번째 선택 매개변수 추가. E2E — 상한 1에서 첫 클라 정상 동작·초과 접속 즉시 거부 + 음수 거부, 팩토리 단위 4건. 테스트 87→91건 통과.
- [[../03-Reference/Public-API|Public-API]] 헬퍼 표, [[../01-Overview/Feature-Spec|Feature-Spec]] F7 갱신.
- **`v2.3.0` 릴리스** — 태그 푸시 → run 34150180755 success. 누적 단위: `MaxPendingCalls`(2차) + `maxConnections` 오버로드(3차) — 리소스 고갈 방어 3종 세트. 게시 상태·README 표기 갱신.

## 2026-09-08 (2차)

- **`HubBase.MaxPendingCalls` — outgoing 대기 테이블 상한** — 스펙 개선 영역 1(보안·리소스 고갈 「대기 호출 적체」) 처리. 응답 불능 피어에 대해 호출자가 무한정 쌓는 대기 CallId(메모리 고갈 표면)를 상한으로 끊는다. 기본 0(무제한·동작 불변), 도달 시 새 호출은 대기 없이 즉시 `InvalidOperationException`(fail-fast), 슬롯 해제(응답·오류·타임아웃·끊김) 후 재시도 가능, 음수 거부. 검사·등록 경쟁의 순간적 초과는 근사 강제로 문서화. 단위 테스트 3건(상한 fail-fast·슬롯 해제 재시도·기본 무제한·음수 거부). 테스트 84→87건 통과.
- **PENDING 해소·정리** — v2.2.0 nupkg blob 5개 패키지 전부 확인(예전 404는 URL 형식 오류 — `{id}.{version}.nupkg` 로 206). LSP 캐시 아티팩트 진단 7건 `lens_diagnostic_mark` false-positive 처리, [[../00-AI/PENDING|PENDING]] 항목 정리.
- [[../03-Reference/Public-API|Public-API]] HubBase 멤버 표, [[../01-Overview/Feature-Spec|Feature-Spec]] F2 outgoing·수용 기준 갱신.

## 2026-09-08

- **형제 스택 채택 — Communication 2.0.1·MessageProtocol 2.3.0** — 하위 신기능 채택 절차(스펙 § 하위 프로젝트 채택) 최초 실행. `CommunicationPackageVersion` 2.0.0→2.0.1(RUDP 메시지 채널 흐름제어 실패폐쇄·MaxFrameLength 수신 적용·peer id 재사용 채널 오염 수정·폴링 오류 추적 스로틀·`ConnectTimeout` 신설), `MessageProtocolPackageVersion` 2.1.0→2.3.0(공유 참조 동일성·중복 (MessageId, ClassId) 컴파일 타임 거부·백레퍼런스 가이드 예외·등록 순서 검증).
- **`RpcClient.ConnectAsync` 연결 타임아웃 오버로드** — 신설 `connectTimeoutMs` 로 침묵 호스트(패킷 블랙홄) 연결 실패를 상한 이내로 확정. 기존 시그니처는 그대로(기본값 유지·하위호환), `HubSessionFactory.CreateTransportOptions` 도 `connectTimeoutMs` 선택 매개변수 추가(0=미설정, 음수 거부). E2E 회귀 — 침묵 포트 300ms 상한 실패(3초 미만 완료) + 팩토리 계약 단위 5건.
- **E2E 포트 할당 임시 포트 전환** — 고정 시드(9600+7n)가 Windows 예약 포트 범위·선행 실행 잔여 리스너와 충돌해 "RUDP 리스너 바인딩 실패" 플레이크(9607·9621·9705 관측) — OS 배정 임시 포트(UdpClient(0) 확보)로 교체. 테스트 77→84건 통과.
- [[../00-AI/CONTEXT|CONTEXT]]·[[../01-Overview/Feature-Spec|Feature-Spec]] 상태·버전 동기화, [[../03-Reference/Public-API|Public-API]] 헬퍼 표 갱신.
- **`v2.2.0` 릴리스** — 태그 푸시 → run 34148443218 success, 5개 패키지 flatcontainer 인덱스 2.2.0 등재 확인. 게시 상태·README 버전 표기 갱신, [[../00-AI/PENDING|PENDING]] 신규(LSP 캐시 아티팩트·nupkg blob 유입 지연 관측).

## 2026-09-07

- **`v2.1.0` 릴리스 · NuGet 게시**: `Source/Directory.Build.props` `<Version>` 2.1.0 → 태그 `v2.1.0` 푸시 →
  run 34136624883 success, `DRPC.{Attribute,Shared,Client,Server,CodeGenerator}` 2.1.0 flatcontainer 등록·nupkg HTTP 200 확인.
  README(기능 목록에 제네릭 프로시저·패키지 버전·테스트 수 77), `CONTEXT`(릴리스 상태), `Feature-Spec`(게시 상태 2.1.0 절),
  `Public-API`(버전 표기 2.1.0) 갱신.

- **F12 제네릭 프로시저 구현** — `[GenericProcedure(params Type[]) / (int slot, params Type[])]` 속성 신규(DRPC.Attribute, AllowMultiple). 4가지 형태 지원: 반환 전용 `T Proc<T>()`·매개변수 `void Proc<T>(T v)`·복합 `T1 Proc<T1,T2,T3>(T2,T3)`(데카르트 곱, 64구성 상한)·`[GenericMessage]` 파라미터 `void Proc<T>(Package<T>)`(미선언 슬롯은 메시지 구성 선언에서 상속). 호출은 일반 프로시저와 동일(`hub.XxxAsync(42)` 추론 / `hub.XxxAsync<int>()` 명시), 서버 구현은 `Task<T> Xxx_Implementation<T>(...)` partial. 와이어: 페이로드 첫 4바이트 구성 인덱스(슬롯 0이 가장 느린 오도미터) — 포맷·HubBase 불변. 진단: DRPCGEN007(슬롯 미선언)·008(호출 지점 미선언 타입 인자 — 미해결 `{Method}Async` 호출 구조적 탐지, 추론 불가 슬롯은 런타임 백스톱에 위임)·009(무효 선언·[GenericMessage] 슬롯의 비-ID-헤더 타입·타입 파라미터 제약·상한). 런타임 백스톱: 스텁 `else` throw + 수신측 알 수 없는 구성 인덱스 → `RpcErrorCode.Unhandled`.
- 형제 의존 승격: `MessageProtocol` 2.0.0 → **2.1.0**(GenericMessage 포함 안정판, 형제 저장소 태그 `v2.1.0` 게시 후 핀). Communication 2.0.0 유지.
- 구현 중 발견·승격된 제약: MessageProtocol 제네릭 구성은 T 멤버를 런타임 메시지 디스패치로 직렬화 → `[GenericMessage]` 슬롯 타입은 ID 헤더 메시지(Standalone/Group)여야 하고 NonId·프리미티브는 런타임 크래시 대신 DRPCGEN009 로 컴파일 타임 거부.
- 테스트 58→77(CodeGenerator 제네릭 18케이스 포함 36·E2E 루프백 5케이스 추가 22·Shared 19). Sandbox 데모에 4형태 호출 추가(GetConfig/Describe/Blend/Unwrap+GiftBox). `Feature-Spec` F12 절·F1·F5, `Public-API` 속성·스텁 서명·진단, `CONTEXT` 상태 갱신.

## 2026-09-05

- **F8 완료 · `v2.0.0` NuGet 게시**: 기존 `.github/workflows/nuget-publish.yml`(`v*` 트리거)에 태그를 붙여 푸시 →
  run 33979153588 success, `DRPC.{Attribute,Shared,Client,Server,CodeGenerator}` 2.0.0 이 등록·다운로드까지 확인.
  F8 을 "범위 밖/미구현"으로 적었던 문서(Feature-Spec·CONTEXT·Home·Known-Issues)를 실제에 맞게 정정하고,
  NuGet 유입 지연·`NU5128` 정상 경고·워크플로 공백 실패 가드를 Known-Issues 에 추가.
- 재구축 F1–F7·F9·F11 구현 완료에 맞춰 문서 신규: `02-Architecture/Overview.md`(패키지 그래프·호출 흐름·페이로드 인코딩),
  `03-Reference/Public-API.md`(표면·NuGet 2.0.0 고정·오류 코드·진단·빌드 명령), `04-Guides/Getting-Started.md`,
  `06-Troubleshooting/Known-Issues.md`.
- `05-Decisions/0002-async-only-delivery-and-payload.md` 신규: Async 전용 스텁, DRPC 자체 `RpcDeliveryMode` + 매핑 경계,
  one-way = `CallId 0`, 메서드별 래퍼 타입 폐기(flat 페이로드). 4결정 기록.
- `01-Overview/Feature-Spec.md` 갱신: 위 결정을 F1·F3·F5·F6·F7·F8·F9·F11 에 반영, 구현 상태 절 추가(테스트 58개),
  오픈 이슈 1 해소로 "없음", 스테일했던 전송 열거형 명세 제거, `MethodReliableTypes` → `MethodDeliveryModes` 개칭 반영.
- `00-AI/CONTEXT.md` 현 상태 갱신(초기 스켈레톤 → 구현 완료, `-c Release` 사용 이유, 빌드·테스트·샌드박스 명령),
  `01-Overview/Home.md` MoC 확장.
- `06-Troubleshooting/Known-Issues.md` 에 함정 두 개 추가: `EmitCompilerGeneratedFiles` 전역 플래그가 참조 프로젝트 obj 에
  생성 코드를 덤프해 `CS0102`·`CS0111` 중복 정의 오류를 내는 문제(경험 기록)와, 새로 만든 프로젝트의 restore 전 LSP 가짜 오류.
- `Test/DRPC.E2E.Tests/GeneratedShapeTests.cs` 신규: 생성 산출물 형태를 리플렉션으로 검증 (Async 전용·접속 팩토리 서명·
  구현 후킹 배치) — 파일 grep 대신 어셈블리를 보아 테스트 횟수 53 → 58.

## 2026-08-31

- `05-Decisions/0001-hub-naming-and-version-2.md` 신규: Hub 명명 소유 주체 기준 정렬(`ClientHub`/`ServerHub`) + `2.0.0` 출발 결정.
- `01-Overview/Feature-Spec.md` 갱신: 명명·버전 결정 반영, sync Outgoing은 오픈 이슈로 보류.
- `01-Overview/Feature-Spec.md` 신규: 레거시 패리티 기반 재구축 기능 명세(F1–F11, 재구축 결정, 오픈 이슈).
- `01-Overview/Home.md` 신규: 사람용 진입점(스텁).
- `00-AI/CONTEXT.md`, `00-AI/CONVENTIONS.md` 신규: 에이전트 진입점·작성 규약(레거시 승계).
