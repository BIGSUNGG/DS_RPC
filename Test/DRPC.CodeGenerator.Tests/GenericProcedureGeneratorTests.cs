using Microsoft.CodeAnalysis;
using Xunit;

namespace DRPC.CodeGenerator.Tests;

/// <summary>
/// 제네릭 프로시저([GenericProcedure]) 생성·진단 검사.
/// 와이어 계약: 페이로드 첫 4바이트 = 구성 인덱스(데카르트 곱, 슬롯 0이 가장 느리게 도는 순서).
/// </summary>
public class GenericProcedureGeneratorTests
{
    const string ReturnOnlyContract =
        """
        [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 7)]
        [GenericProcedure(typeof(int), typeof(string))]
        T GetDefault<T>();
        """;

    [Fact]
    public void Return_only_generic_stub_emits_typeof_dispatch()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(ReturnOnlyContract));

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("public async global::System.Threading.Tasks.Task<T> GetDefaultAsync<T>()", result.GeneratedSource);
        Assert.Contains("if (typeof(T) == typeof(global::System.Int32))", result.GeneratedSource);
        Assert.Contains("if (typeof(T) == typeof(global::System.String))", result.GeneratedSource);
        Assert.Contains("byte[] __payload = __WriteParams_ITestServerProcedures_GetDefault_0();", result.GeneratedSource);
        Assert.Contains("return (T)(object)__ReadReturn_ITestServerProcedures_GetDefault_1(__response);", result.GeneratedSource);
        Assert.Contains("is not declared with [GenericProcedure]", result.GeneratedSource);
    }

    [Fact]
    public void Construction_index_is_written_first_and_read_back_on_server()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ServerHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 7)]
            [GenericProcedure(typeof(int), typeof(string))]
            T GetDefault<T>();
            """,
            hubBody: "private partial Task<T> GetDefault_Implementation<T>() => Task.FromResult<T>(default!);"));

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("__buf.WriteInt32(0);", result.GeneratedSource);
        Assert.Contains("__buf.WriteInt32(1);", result.GeneratedSource);
        Assert.Contains("int __ci = __rd.ReadInt32();", result.GeneratedSource);
        Assert.Contains("switch (__ci)", result.GeneratedSource);
        Assert.Contains("unknown generic construction index", result.GeneratedSource);
    }

    [Fact]
    public void Multi_slot_generic_emits_cartesian_arms_and_closed_implementation_call()
    {
        string contract =
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 8)]
            [GenericProcedure(0, typeof(int), typeof(string))]
            [GenericProcedure(1, typeof(float), typeof(double))]
            T1 Pick<T1, T2>(T2 low, T2 high);
            """;

        // typeof 디스패치 암(arm)은 호출 쪽(클라이언트 허브의 outgoing 스텁)에 나온다.
        var client = GeneratorHarness.Run(GeneratorHarness.ClientHub(contract));

        Assert.Empty(client.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // 4구성(슬롯 0이 느리게): (int,float)(int,double)(string,float)(string,double).
        Assert.Contains("typeof(T1) == typeof(global::System.Int32) && typeof(T2) == typeof(global::System.Single)", client.GeneratedSource);
        Assert.Contains("typeof(T1) == typeof(global::System.String) && typeof(T2) == typeof(global::System.Double)", client.GeneratedSource);
        Assert.Contains("__WriteReturn_ITestServerProcedures_Pick_3", client.GeneratedSource);

        // 구성별 닫힌 구현 호출·partial 서명은 수신 쪽(서버 허브)에 나온다.
        var server = GeneratorHarness.Run(GeneratorHarness.ServerHub(contract,
            hubBody: "private partial Task<T1> Pick_Implementation<T1, T2>(T2 low, T2 high) => Task.FromResult<T1>(default!);"));

        Assert.Empty(server.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("await Pick_Implementation<global::System.Int32, global::System.Single>(low, high)", server.GeneratedSource);
        // 구현 partial 서명에 타입 파라미터가 붙는다.
        Assert.Contains("private partial global::System.Threading.Tasks.Task<T1> Pick_Implementation<T1, T2>(T2 low, T2 high);", server.GeneratedSource);
    }

    [Fact]
    public void Parameter_generic_stub_casts_closed_arguments()
    {
        string contract =
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 9)]
            [GenericProcedure(typeof(int), typeof(string))]
            void Log<T>(T value);
            """;

        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(contract));

        Assert.Contains("public async global::System.Threading.Tasks.Task LogAsync<T>(T value)", result.GeneratedSource);
        Assert.Contains("__WriteParams_ITestServerProcedures_Log_1(((global::System.String)(object)value!))", result.GeneratedSource);
    }

    [Fact]
    public void GenericMessage_parameter_derives_slot_from_constructions()
    {
        string source = GeneratorHarness.ClientHub(
                """
                [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 10)]
                void Deliver<T>(Package<T> box);
                """)
            + """
              [MessageProtocol.StandaloneMessage(9)]
              public partial class Payload
              {
                  public int Id { get; set; }
              }

              [MessageProtocol.StandaloneMessage(50)]
              [MessageProtocol.GenericMessage(typeof(Package<Payload>), ClassId = 1)]
              public partial class Package<T>
              {
                  public T Value { get; set; } = default!;
              }
              """;

        var result = GeneratorHarness.Run(source);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("if (typeof(T) == typeof(global::Payload))", result.GeneratedSource);
        // 닫힌 Package<Payload> 는 ID 헤더(object dispatch) 직렬화를 쓴다.
        Assert.Contains("MessageSerializer.SerializeToWriter", result.GeneratedSource);
    }

    [Fact]
    public void DRPCGEN009_when_generic_message_slot_type_is_not_a_message()
    {
        // MessageProtocol 은 [GenericMessage] 의 T 멤버를 런타임 메시지 디스패치로 직렬화한다 —
        // int/string 같은 비메시지 타입은 구성 자체가 무효(런타임 크래시를 컴파일 타임 승격).
        string source = GeneratorHarness.ClientHub(
                """
                [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 10)]
                void Deliver<T>(Package<T> box);
                """)
            + """
              [MessageProtocol.StandaloneMessage(50)]
              [MessageProtocol.GenericMessage(typeof(Package<int>), ClassId = 1)]
              public partial class Package<T>
              {
                  public T Value { get; set; } = default!;
              }
              """;

        var result = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(result.WithId("DRPCGEN009"));
        Assert.Contains("ID header", diagnostic.GetMessage());
    }

    [Fact]
    public void Generic_contract_with_implementations_compiles()
    {
        // 클라이언트 허브가 구현하는 건 클라이언트 계약(Echo). GetDefault 는 서버 계약(송신 스텁만)이다.
        string source = GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 7)]
            [GenericProcedure(typeof(int), typeof(string))]
            T GetDefault<T>();
            """,
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 0)]
            [GenericProcedure(typeof(int), typeof(string))]
            T Echo<T>(T value);
            """,
            """
            private partial Task<T> Echo_Implementation<T>(T value) => Task.FromResult(value);
            """);

        var result = GeneratorHarness.Run(source);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(result.CompileErrors());
    }

    // ── 진단 ───────────────────────────────────────────────────────────

    [Fact]
    public void DRPCGEN007_when_generic_parameter_is_undeclared()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] T Bad<T>(T value);"));

        Assert.True(result.HasDiagnostic("DRPCGEN007"));
        Assert.DoesNotContain("MethodCallActions.Add", result.GeneratedSource);
    }

    [Fact]
    public void DRPCGEN009_when_generic_procedure_on_non_generic_method()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)]
            [GenericProcedure(typeof(int))]
            int NotGeneric(int value);
            """));

        Assert.True(result.HasDiagnostic("DRPCGEN009"));
    }

    [Fact]
    public void DRPCGEN009_when_slot_is_out_of_range_or_duplicated()
    {
        var outOfRange = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)]
            [GenericProcedure(1, typeof(int))]
            T Bad<T>(T value);
            """));
        Assert.True(outOfRange.HasDiagnostic("DRPCGEN009"));

        var duplicated = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)]
            [GenericProcedure(typeof(int))]
            [GenericProcedure(typeof(string))]
            T Bad<T>(T value);
            """));
        Assert.True(duplicated.HasDiagnostic("DRPCGEN009"));
    }

    [Fact]
    public void DRPCGEN009_when_type_parameter_is_constrained()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)]
            [GenericProcedure(typeof(int))]
            T Bad<T>(T value) where T : class;
            """));

        Assert.True(result.HasDiagnostic("DRPCGEN009"));
    }

    [Fact]
    public void DRPCGEN008_when_explicit_type_argument_is_undeclared()
    {
        string source = GeneratorHarness.ClientHub(ReturnOnlyContract)
            + """
              public static class Caller
              {
                  public static async System.Threading.Tasks.Task Call(TestClientHub hub)
                      => await hub.GetDefaultAsync<System.DateTime>();
              }
              """;

        var result = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(result.WithId("DRPCGEN008"));
        Assert.Contains("System.DateTime", diagnostic.GetMessage());
    }

    [Fact]
    public void DRPCGEN008_when_inferred_type_argument_is_undeclared()
    {
        string source = GeneratorHarness.ClientHub(
                """
                [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 7)]
                [GenericProcedure(typeof(int), typeof(string))]
                void Log<T>(T value);
                """)
            + """
              public static class Caller
              {
                  public static async System.Threading.Tasks.Task Bad(TestClientHub hub)
                      => await hub.LogAsync(3.5);

                  public static async System.Threading.Tasks.Task Ok(TestClientHub hub)
                      => await hub.LogAsync(42);
              }
              """;

        var result = GeneratorHarness.Run(source);

        Assert.Single(result.WithId("DRPCGEN008"));
    }

    [Fact]
    public void Non_generic_stub_calls_do_not_trigger_DRPCGEN008()
    {
        string source = GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 3)] int Add(int a, int b);")
            + """
              public static class Caller
              {
                  public static async System.Threading.Tasks.Task Call(TestClientHub hub)
                      => await hub.AddAsync(2, 3);
              }
              """;

        var result = GeneratorHarness.Run(source);

        Assert.False(result.HasDiagnostic("DRPCGEN008"));
    }
}
