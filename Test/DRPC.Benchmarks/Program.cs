using BenchmarkDotNet.Running;

namespace DRPC.Benchmarks;

/// <summary>
/// DRPC 런타임 핫패스 마이크로벤치마크 — FakeSession 으로 네트워크 없이 허브 런타임만 측정한다.
/// 실행: dotnet run -c Release --project Test/DRPC.Benchmarks [-- --filter *Roundtrip*]
/// </summary>
public static class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
