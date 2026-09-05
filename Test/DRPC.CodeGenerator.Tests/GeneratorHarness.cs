using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DRPC.CodeGenerator.Tests;

/// <summary>
/// 생성기를 in-memory 컴파일레이션 위에서 직접 구동해 진단과 생성 결과를 검사한다.
/// </summary>
internal static class GeneratorHarness
{
    static GeneratorHarness()
    {
        References = BuildReferences();
    }

    /// <summary>테스트 대상 컴파일레이션이 참조할 어셈블리(DRPC·MessageProtocol·Communication 포함).</summary>
    public static readonly ImmutableArray<MetadataReference> References;

    static ImmutableArray<MetadataReference> BuildReferences()
    {
        // TPA(런셋 프레임워크) + 출력 디렉터리 어셈블리를 파일명 기준으로 합친다(중복 참조 방지).
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string tpa = (string)(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty);
        foreach (string path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            byName[Path.GetFileName(path)] = path;
        }

        foreach (string path in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            byName[Path.GetFileName(path)] = path;
        }

        return byName.Values.Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToImmutableArray();
    }

    public static GeneratorResult Run(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "DrpcGenTests",
            new[] { tree },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator generator = new RpcIncrementalGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());

        // 구동은 새 드라이버를 반환한다 — 반환값을 버리면 빈 결과를 읽게 된다.
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation updated, out _);
        GeneratorDriverRunResult result = driver.GetRunResult();

        return new GeneratorResult(
            // 진단이 하나도 없으면 Diagnostics 가 default ImmutableArray 로 온다(IsDefault 검사 필수).
            result.Results
                .SelectMany(r => r.Diagnostics.IsDefault
                    ? Enumerable.Empty<Diagnostic>()
                    : r.Diagnostics)
                .ToImmutableArray(),
            string.Concat(result.GeneratedTrees.Select(static t => t.ToString())),
            updated);
    }

    /// <summary>서버·클라이언트 계약 본문을 갈아 끼워 클라이언트 측 허브 소스를 만든다.</summary>
    public static string ClientHub(string serverContractBody, string clientContractBody = "", string hubBody = "")
        => $$"""
           using DRPC;
           using DRPC.Client.Network;
           using DRPC.Shared.Interface;
           using MessageProtocol;
           using System;
           using System.Collections.Generic;
           using System.Threading.Tasks;

           public interface ITestServerProcedures : IServerProcedureDeclarations
           {
           {{serverContractBody}}
           }

           public interface ITestClientProcedures : IClientProcedureDeclarations
           {
           {{clientContractBody}}
           }

           public partial class TestClientHub : ClientHub<ITestServerProcedures, ITestClientProcedures>
           {
           {{hubBody}}
           }
           """;

    /// <summary>서버 측 허브 소스(리스닝 배선 확인용).</summary>
    public static string ServerHub(string serverContractBody, string clientContractBody = "", string hubBody = "")
        => $$"""
           using DRPC;
           using DRPC.Server.Network;
           using DRPC.Shared.Interface;
           using MessageProtocol;
           using System;
           using System.Collections.Generic;
           using System.Threading.Tasks;

           public interface ITestServerProcedures : IServerProcedureDeclarations
           {
           {{serverContractBody}}
           }

           public interface ITestClientProcedures : IClientProcedureDeclarations
           {
           {{clientContractBody}}
           }

           public partial class TestServerHub : ServerHub<ITestServerProcedures, ITestClientProcedures>
           {
           {{hubBody}}
           }
           """;

    public sealed record GeneratorResult(ImmutableArray<Diagnostic> Diagnostics, string GeneratedSource, Compilation Compilation)
    {
        public bool HasDiagnostic(string id) => Diagnostics.Any(d => d.Id == id);

        public ImmutableArray<Diagnostic> WithId(string id) => Diagnostics.Where(d => d.Id == id).ToImmutableArray();

        /// <summary>생성 코드가 그 자체로 컴파일되는지(멤버·타입 참조가 맞는지) 본다.</summary>
        public ImmutableArray<Diagnostic> CompileErrors()
            => Compilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)
                .Where(static d => d.Id != "DRPCGEN004")
                .ToImmutableArray();
    }
}
