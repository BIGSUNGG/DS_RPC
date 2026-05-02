# DS_RPC (DRPC)

분산 RPC 및 RUDP 기반 통신을 위한 **.NET 라이브러리** 모음입니다. 라이브러리는 **.NET Standard 2.1**을 타깃으로 하여 **Unity** 등 다양한 클라이언트와 호환됩니다.

## NuGet 패키지

| 패키지 | 설명 |
|--------|------|
| **DRPC.Attribute** | RPC 계약에 사용하는 특성(Attribute) |
| **DRPC.Shared** | 공유 타입·직렬화·MessageProtocol 연동 |
| **DRPC.Client** | 클라이언트 측 RPC 및 RUDP 클라이언트 연동 |
| **DRPC.Server** | 서버 측 RPC 및 RUDP 서버 연동 |
| **DRPC.CodeGenerator** | Roslyn 분석기/소스 생성기(DRPC·계약 코드 생성) |

NuGet.org에서 패키지 ID로 검색해 설치할 수 있습니다. 서버/클라이언트 조합에 맞게 **Shared**와 **Attribute**를 기준으로 필요한 패키지를 선택하세요.

## 요구 의존성

런타임 패키지는 **MessageProtocol**, **Communication**(RUDP) 등 외부 NuGet 패키지 버전을 사용합니다. 버전은 저장소 루트 [Directory.Build.props](Directory.Build.props)의 `MessageProtocolPackageVersion`, `CommunicationPackageVersion`에서 관리합니다.

## 예제

동작 예제는 `Examples/Sandbox.Contracts`, `Examples/Sandbox.Server`, `Examples/Sandbox.Client`를 참고하세요.

## 빌드·패키징

```bash
dotnet build DRPC.slnx
dotnet pack Source/DRPC.Shared/DRPC.Shared.csproj -c Release -p:Version=1.0.0 -o ./artifacts
```

태그 `v1.2.3` 푸시 시 GitHub Actions가 동일 버전(`1.2.3`)으로 라이브러리 패키지를 빌드·게시합니다. 저장소 시크릿 `NUGET_API_KEY`가 필요합니다.

## 저장소

https://github.com/BIGSUNGG/DS_RPC
