---
project: DS_RPC
type: reference
status: stable
tags: [reference, performance, benchmark]
updated: 2026-09-09
---

# Performance — 핫패스 기준선

`Test/DRPC.Benchmarks`(BenchmarkDotNet) — 루프백 메모리 세션으로 **DRPC 런타임만** 측정(전송 스택 비용 제외). 실행:

```powershell
dotnet run -c Release --project Test/DRPC.Benchmarks -- --filter "*" --job short
```

## 기준선 (2026-09-09, v2.9.1, net10.0, ShortRun)

| 벤치마크 | 평균 | 할당/오퍼레이션 |
| ----------- | ------ | ----------------- |
| Outgoing roundtrip (loopback session) — `RequestRPC` 송신→루프백 응답→대기 완성 | ~222 ns | 384 B |
| Incoming dispatch — 수신 요청→구현→응답 송신 | ~109 ns | 312 B |
| One-way send (CallId 0) — `SendRPC` | ~21 ns | 32 B |
| Converter serialize (RPC request) — MessageProtocol 직렬화+IBufferWriter 단일 복사 | ~58 ns | 344 B |

## 읽는 법

- 왕복 384B = 요청/응답 메시지 객체 2 + TCS + 등록 해지 등 런타임 필수 할당. 10차(v2.9.1)의 중간 배열 제거 전에는 변환기 경로가 여기에 +배열 1건이었음.
- 수치는 ShortRun(빠른 기준선용) — 정밀 비교 시 기본 잡으로 재측정.
- 영역 3(성능) 개선은 이 기준선 대비 유의미한 개선이 있을 때만 수행 — 근거 없는 최적화 금지(스펙 원칙).

## 변경 이력

- 2026-09-09 최초 기준선 확정(v2.9.1, 10차 핫패스 최적화 직후).
