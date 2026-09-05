using System.Reflection;
using DRPC.Shared.Interface;
using Xunit;

namespace DRPC.E2E.Tests;

/// <summary>
/// 생성 산출물의 **형태**를 실제 어셈블리 리플렉션으로 검증한다.
/// (MSBuild 의 EmitCompilerGeneratedFiles 로 파일을 뽑아 grep 하는 방식은
///  참조 프로젝트까지 프로퍼티가 전파되어 MessageProtocol 생성이 중복되는 함정이 있다 — 여기가 더 강하다.)
/// </summary>
public class GeneratedShapeTests
{
    static readonly Type[] Hubs = { typeof(E2EClientHub), typeof(E2EServerHub) };

    static readonly MethodInfo[] ContractMethods =
        typeof(IServerProcedures).GetMethods().Concat(typeof(IClientProcedures).GetMethods()).ToArray();

    const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    public void Every_contract_method_has_an_async_stub_and_no_sync_stub()
    {
        foreach (MethodInfo contract in ContractMethods)
        {
            Assert.Contains(Hubs, hub => hub.GetTypeInfo().GetDeclaredMethod(contract.Name + "Async") is not null);

            foreach (Type hub in Hubs)
            {
                // sync 스텁(계약과 같은 이름)은 존재하면 안 된다 — Async 전용 결정(ADR-0002)의 회귀 방지.
                Assert.Null(hub.GetTypeInfo().GetDeclaredMethod(contract.Name));
            }
        }
    }

    [Fact]
    public void No_member_is_marked_obsolete()
    {
        foreach (Type hub in Hubs)
        {
            Assert.DoesNotContain(hub.GetMembers(PublicInstance | BindingFlags.Static),
                m => m.GetCustomAttribute<ObsoleteAttribute>() is not null);
        }
    }

    [Fact]
    public void Connection_factories_are_generated_with_connectionkey_overloads()
    {
        // 생성 멤버라 nameof 로 참조하지 않는다 — 생성이 깨지면 테스트 하나가 실패해야지
        // 테스트 어셈블리 전체가 컴파일되지 않는 것은 진단에 불리하다.
        MethodInfo? connect = typeof(E2EClientHub).GetMethod(
            "ConnectAsync",
            new[] { typeof(string), typeof(int), typeof(string), typeof(CancellationToken) });
        Assert.NotNull(connect);
        Assert.True(connect!.ReturnParameter.ParameterType.IsGenericType); // Task<THub>

        MethodInfo? listen = typeof(E2EServerHub).GetMethod(
            "ListenAsync",
            new[] { typeof(int), typeof(string), typeof(Func<E2EServerHub, Task>), typeof(CancellationToken) });
        Assert.NotNull(listen);
    }

    static bool HasDeclared(Type hub, string name)
        => hub.GetTypeInfo().GetDeclaredMethods(name).Any();

    [Fact]
    public void Incoming_contract_methods_get_implementation_hooks_on_the_owning_side()
    {
        // 서버 계약은 서버 허브가, 클라이언트 계약은 클라이언트 허브가 구현 후킹을 받는다.
        // (컴파일 후 partial 정의/구현은 하나의 메서드로 합쳐지므로 이름 존재 여부로 본다.)
        Assert.True(HasDeclared(typeof(E2EServerHub), "Add_Implementation"));
        Assert.True(HasDeclared(typeof(E2EServerHub), "PlaceOrder_Implementation"));
        Assert.True(HasDeclared(typeof(E2EClientHub), "ClientValue_Implementation"));
        Assert.True(HasDeclared(typeof(E2EClientHub), "ReceiveLine_Implementation"));

        Assert.False(HasDeclared(typeof(E2EServerHub), "ClientValue_Implementation"));
        Assert.False(HasDeclared(typeof(E2EClientHub), "Add_Implementation"));
    }

    [Fact]
    public void Contract_markers_are_respected_by_the_generator()
    {
        // 마커 인터페이스를 상속한 계약만 허브 형식 인자로 허용된다(생성기 DRPCGEN002 의 근거).
        Assert.True(typeof(IServerProcedureDeclarations).IsAssignableFrom(typeof(IServerProcedures)));
        Assert.True(typeof(IClientProcedureDeclarations).IsAssignableFrom(typeof(IClientProcedures)));
        Assert.False(typeof(IServerProcedureDeclarations).IsAssignableFrom(typeof(IClientProcedures)));
    }
}
