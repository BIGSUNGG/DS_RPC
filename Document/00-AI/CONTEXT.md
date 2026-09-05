---
project: DS_RPC
type: context
status: stable
tags: [ai, context]
updated: 2026-09-05
---

# CONTEXT — 에이전트 진입점

재구축 중인 DS_RPC 저장소. 작업 시작 시 이 문서를 먼저 읽는다.

## 현 상태 (2026-09-05)

- **재구축 F1–F7·F9·F11 구현 완료.** `Source/` 5개 패키지(Attribute·Shared·CodeGenerator·Client·Server), `Sandbox/` 3개, `Test/` 3계층(58개 통과).
- 형제 스택은 **NuGet 2.0.0 안정판만** 참조한다(`MessageProtocol`, `Communication.Network.RUDP.*`) — 형제 저장소 프로젝트 참조·하드 경로 없음.
- 저장소 루트 솔루션은 `DRPC.slnx`.
- 빌드·테스트는 **`-c Release`** 를 쓴다. `Debug` 는 언어 서버가 생성기 DLL 을 점유해 복사가 실패할 수 있다([[../06-Troubleshooting/Known-Issues|Known-Issues]]).
- 구현 범위·수용 기준의 권위 문서는 [[../01-Overview/Feature-Spec|Feature-Spec]]. 설계 결정은 [[../05-Decisions/0001-hub-naming-and-version-2|ADR-0001]], [[../05-Decisions/0002-async-only-delivery-and-payload|ADR-0002]].
- 미구현: F10 TemplateSource. (F8 는 완료 — `v2.0.0` 태그로 5개 패키지 NuGet 게시 확인)
- 레거시 코드·문서는 `Legacy/` 아카이브. 동작 근거가 필요하면 레거시를 참고하되 **구현 대상은 Feature-Spec** 이다.

```powershell
dotnet build DRPC.slnx -c Release
dotnet test  DRPC.slnx -c Release
dotnet run --no-build -c Release --project Sandbox/Sandbox.Server   # + Client 별도 창
```

## 규칙

1. `Source/`, `Test/`, `Sandbox/`, `TemplateSource/` 변경 시 같은 턴에 `Document/` 갱신.
2. 문서 작성 규약은 [[../00-AI/CONVENTIONS|CONVENTIONS]].
3. 레거시 문서 링크는 **상대 경로 + 별칭** 형식만 사용(단축 링크는 Legacy/Document와 파일명 충돌로 Ambiguous).

## 관련

- [[../00-AI/CONVENTIONS|CONVENTIONS]]
- [[../01-Overview/Home|Home]]
