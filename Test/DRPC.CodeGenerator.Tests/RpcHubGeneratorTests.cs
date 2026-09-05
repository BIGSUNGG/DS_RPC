using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace DRPC.CodeGenerator.Tests;

/// <summary>
/// 생성기 진단(DRPCGEN001–006)과 생성 결과의 형태를 검사한다.
/// 레거시 대비 확정 동작: Outgoing 은 <c>{Method}Async</c> 만 생성한다(sync [Obsolete] 스텁 없음).
/// </summary>
public class RpcHubGeneratorTests
{
    const string AddContract = "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 3)] int Add(int value1, int value2);";

    [Fact]
    public void Outgoing_generates_async_only_stub()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(AddContract));

        Assert.Contains("public async global::System.Threading.Tasks.Task<global::System.Int32> AddAsync(global::System.Int32 value1, global::System.Int32 value2)", result.GeneratedSource);
        Assert.Contains("RequestRPC(3, __payload, global::DRPC.RpcDeliveryMode.ReliableOrdered)", result.GeneratedSource);
        Assert.DoesNotContain("[global::System.Obsolete", result.GeneratedSource);
        Assert.DoesNotContain("public int Add(", result.GeneratedSource);
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Omitted_mode_defaults_to_reliable_ordered()
    {
        // 인자 없는 [RemoteProcedure] = 기본 ReliableOrdered (MethodId 는 선언 순서 → DRPCGEN004 경고).
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub("[RemoteProcedure] int A();"));

        Assert.Contains("RequestRPC(0, __payload, global::DRPC.RpcDeliveryMode.ReliableOrdered)", result.GeneratedSource);
        Assert.True(result.HasDiagnostic("DRPCGEN004"));
    }

    [Fact]
    public void Per_method_delivery_override_is_emitted()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.Unreliable, 4)] void Ping(int seq);"));

        Assert.Contains("RequestRPC(4, __payload, global::DRPC.RpcDeliveryMode.Unreliable)", result.GeneratedSource);
    }

    [Fact]
    public void OneWay_uses_send_rpc_and_skips_response_read()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 5, OneWay = true)] void Note(string text);"));

        Assert.Contains("await SendRPC(5, __payload, global::DRPC.RpcDeliveryMode.ReliableUnordered)", result.GeneratedSource);
        Assert.DoesNotContain("OneWayMethodIds", result.GeneratedSource);
    }

    [Fact]
    public void Void_without_oneway_still_waits_for_empty_response()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 6)] void Do();"));

        Assert.Contains("await RequestRPC(6, __payload", result.GeneratedSource);
    }

    [Fact]
    public void Incoming_generates_dispatch_and_partial_implementation_hook()
    {
        string source = GeneratorHarness.ServerHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int Compute(int value);",
            hubBody: "private partial Task<int> Compute_Implementation(int value) => Task.FromResult(value);");

        var result = GeneratorHarness.Run(source);

        Assert.Contains("private async global::System.Threading.Tasks.Task<byte[]> Compute_Requested(byte[] __parameterData)", result.GeneratedSource);
        Assert.Contains("private partial global::System.Threading.Tasks.Task<global::System.Int32> Compute_Implementation(global::System.Int32 value);", result.GeneratedSource);
        Assert.Contains("MethodCallActions.Add(1, Compute_Requested);", result.GeneratedSource);
        Assert.Contains("MethodDeliveryModes[1] = global::DRPC.RpcDeliveryMode.ReliableOrdered;", result.GeneratedSource);
    }

    [Fact]
    public void Server_endpoint_generates_listen_async_and_client_endpoint_connect_async()
    {
        var server = GeneratorHarness.Run(GeneratorHarness.ServerHub(AddContract));
        var client = GeneratorHarness.Run(GeneratorHarness.ClientHub(AddContract));

        Assert.Contains("ListenAsync(int port, string? connectionKey, global::System.Func<TestServerHub, global::System.Threading.Tasks.Task> onConnected", server.GeneratedSource);
        Assert.Contains("global::DRPC.Server.Network.RpcHost.ListenAsync", server.GeneratedSource);
        Assert.Contains("ConnectAsync(string host, int port, string? connectionKey", client.GeneratedSource);
        Assert.Contains("global::DRPC.Client.Network.RpcClient.ConnectAsync", client.GeneratedSource);
    }

    [Fact]
    public void Registration_is_seeded_only_from_incoming_contract()
    {
        // 서버 허브: Incoming = 서버 계약. 클라이언트 계약은 outgoing 이므로 등록하지 않는다.
        var result = GeneratorHarness.Run(GeneratorHarness.ServerHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int In();",
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int Out();"));

        Assert.Contains("MethodCallActions.Add(1, In_Requested);", result.GeneratedSource);
        Assert.DoesNotContain("Out_Requested", result.GeneratedSource);
        Assert.Contains("Task<global::System.Int32> OutAsync()", result.GeneratedSource);
    }

    [Fact]
    public void Primitive_payload_is_flat_concatenation()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 9)] int Mix(int a, string label, float ratio);"));

        Assert.Contains("__buf.WriteInt32(a);", result.GeneratedSource);
        Assert.Contains("__buf.WriteString(label);", result.GeneratedSource);
        Assert.Contains("__buf.WriteSingle(ratio);", result.GeneratedSource);
        Assert.Contains("a = __rd.ReadInt32();", result.GeneratedSource);
    }

    [Fact]
    public void NonId_message_argument_uses_typed_serializer()
    {
        string source = GeneratorHarness.ClientHub(
                "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 8)] void Send(Player player);")
            + """
              [MessageProtocol.NonIdMessage]
              public partial class Player
              {
                  public int Id { get; set; }
              }
              """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains("MessageSerializer.Serialize(player, ref __buf);", result.GeneratedSource);
        Assert.Contains("MessageSerializer.Deserialize<global::Player>(ref __rd)", result.GeneratedSource);
    }

    [Fact]
    public void Group_root_argument_keeps_polymorphic_object_dispatch()
    {
        string source = GeneratorHarness.ClientHub(
                "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 8)] void Send(ChatLine line);")
            + """
              [MessageProtocol.GroupRootMessage(30)]
              public partial class ChatLine
              {
                  public string Text { get; set; } = string.Empty;
              }

              [MessageProtocol.GroupElementMessage(1)]
              public partial class ShoutChatLine : ChatLine
              {
              }
              """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains("MessageSerializer.SerializeToWriter(line, ref __buf);", result.GeneratedSource);
        Assert.Contains("(global::ChatLine)global::MessageProtocol.Serialize.MessageSerializer.DeserializeFromReader(ref __rd)", result.GeneratedSource);
    }

    [Fact]
    public void Collection_payload_emits_length_prefix_and_loop()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 7)] int Sum(List<int> values, byte[] blob);"));

        Assert.Contains("__buf.WriteInt32(values.Count);", result.GeneratedSource);
        Assert.Contains("for (int __i1 = 0; __i1 < values.Count; __i1++)", result.GeneratedSource);
        Assert.Contains("__buf.WriteInt32(blob.Length);", result.GeneratedSource);
        Assert.Contains("__buf.WriteBytes(blob);", result.GeneratedSource);
        Assert.Contains("values = new global::System.Collections.Generic.List<global::System.Int32>(__length1);", result.GeneratedSource);
    }

    [Fact]
    public void Nullable_payload_emits_presence_flag()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 7)] int Opt(int? maybe);"));

        Assert.Contains("__buf.WriteBoolean(maybe.HasValue);", result.GeneratedSource);
        Assert.Contains("if (maybe.HasValue)", result.GeneratedSource);
        Assert.Contains("if (__rd.ReadBoolean())", result.GeneratedSource);
    }

    // ── 진단 ───────────────────────────────────────────────────────────

    [Fact]
    public void DRPCGEN001_when_hub_is_not_partial()
    {
        string source = GeneratorHarness.ClientHub(AddContract).Replace("public partial class TestClientHub", "public class TestClientHub");
        var result = GeneratorHarness.Run(source);

        Assert.True(result.HasDiagnostic("DRPCGEN001"));
        Assert.Empty(result.GeneratedSource);
    }

    [Fact]
    public void DRPCGEN002_when_inheriting_hubbase_directly()
    {
        string source = GeneratorHarness.ClientHub(AddContract).Replace(
            "ClientHub<ITestServerProcedures, ITestClientProcedures>",
            "global::DRPC.Shared.Network.HubBase<ITestServerProcedures, ITestClientProcedures>");
        var result = GeneratorHarness.Run(source);

        Assert.True(result.HasDiagnostic("DRPCGEN002"));
    }

    [Fact]
    public void DRPCGEN003_when_parameter_type_is_unsupported()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int Bad(System.Collections.Generic.Dictionary<string,int> map);"));

        Diagnostic_AssertIds(result, "DRPCGEN003");
    }

    [Fact]
    public void DRPCGEN003_when_contract_returns_task()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] global::System.Threading.Tasks.Task<int> Bad();"));

        Diagnostic_AssertIds(result, "DRPCGEN003");
        Assert.Contains("plain return type", result.WithId("DRPCGEN003").Single().GetMessage());
    }

    [Fact]
    public void DRPCGEN003_when_methods_are_overloaded()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int Same(int a);
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 2)] int Same(string a);
            """));

        Diagnostic_AssertIds(result, "DRPCGEN003");
    }

    [Fact]
    public void DRPCGEN004_when_method_id_is_implicit()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered)] int Implicit();"));

        Microsoft.CodeAnalysis.Diagnostic warning = Assert.Single(result.WithId("DRPCGEN004"));
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("declaration-order MethodId 0", warning.GetMessage());
    }

    [Fact]
    public void DRPCGEN005_when_method_ids_duplicate()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int A();
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1)] int B();
            """));

        Diagnostic_AssertIds(result, "DRPCGEN005");
        Assert.DoesNotContain("MethodCallActions", result.GeneratedSource);
    }

    [Fact]
    public void DRPCGEN006_when_oneway_returns_value()
    {
        var result = GeneratorHarness.Run(GeneratorHarness.ClientHub(
            "[RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 1, OneWay = true)] int Bad();"));

        Diagnostic_AssertIds(result, "DRPCGEN006");
    }

    [Fact]
    public void Generated_hub_compiles_with_user_implementations()
    {
        // 여긴 MessageProtocol 생성기를 같이 돌리지 않으니 메시지 타입은 쓰지 않는다.
        // (DTO 왕복은 DRPC.E2E.Tests 의 RUDP 루프백이 실제 생성기와 함께 검증한다.)
        string source = GeneratorHarness.ClientHub(
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 3)] int Add(int value1, int value2);
            [RemoteProcedure(RpcDeliveryMode.Sequenced, 4)] string Greet(string name);
            [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 5, OneWay = true)] void Note(string text);
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 6)] float[] History(int count, float? last);
            """,
            """
            [RemoteProcedure(RpcDeliveryMode.ReliableOrdered, 0)] int ClientSide(int value);
            """,
            "private partial Task<int> ClientSide_Implementation(int value) => Task.FromResult(value);");

        var result = GeneratorHarness.Run(source);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(result.CompileErrors());
    }

    static void Diagnostic_AssertIds(GeneratorHarness.GeneratorResult result, string id)
    {
        Assert.Contains(id, result.Diagnostics.Select(static d => d.Id));
        // 진단으로 중단된 허브는 스텁을 남기지 않는다.
        Assert.DoesNotContain("MethodCallActions.Add", result.GeneratedSource);
    }
}
