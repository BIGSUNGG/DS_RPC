---
project: DS_RPC
type: guide
status: draft
tags: [guide]
updated: 2026-07-11
---

# Getting Started

## 사전 요구

- .NET Standard 2.1을 지원하는 런타임 또는 Unity (해당 API 호환 버전)
- 예제 실행: .NET 10 SDK (`Sandbox` / `TemplateSource`는 `net10.0`)
- 형제 NuGet: MessageProtocol, Communication (버전은 `Directory.Build.props`)
- 로컬에서 `DRPC.CodeGenerator` 빌드: 형제 저장소 `DS_MessageProtocol`이 `../DS_MessageProtocol`에 있어야 함 ([[Configuration]])

## 빠른 시작 (라이브러리)

1. 저장소 클론 또는 NuGet에서 `DRPC.Attribute` / `Shared` / `Client` 또는 `Server` / `CodeGenerator` 추가
2. [[Packages]]에서 역할에 맞는 패키지 선택
3. 계약 인터페이스 + `partial` Hub 작성 → [[How-To]]

## Sandbox 예제

| 프로젝트 | 경로 |
|----------|------|
| Contracts | `Sandbox/Sandbox.Contracts/` |
| Server | `Sandbox/Sandbox.Server/` |
| Client | `Sandbox/Sandbox.Client/` |

```bash
dotnet build DRPC.slnx
```

1. 서버 실행: `Sandbox/Sandbox.Server` — port `9050`, key `sandbox-key`
2. 클라 실행: `Sandbox/Sandbox.Client` — `ConnectAsync("127.0.0.1", 9050, "sandbox-key", ...)`

클라 Hub: `PlaygroundServerHub` (`ServerHub<,>`). 서버 Hub: `PlaygroundClientHub` (`ClientHub<,>`).

## 관련

- [[How-To]]
- [[Packages]]
- [[Configuration]]
- [[Home]]
