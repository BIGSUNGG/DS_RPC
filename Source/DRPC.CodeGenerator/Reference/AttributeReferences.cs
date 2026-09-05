using Microsoft.CodeAnalysis;

namespace DRPC.CodeGenerator.Reference;

/// <summary>
/// 생성기가 참조하는 타입을 메타데이터 이름으로 해석한다.
/// DRPC·MessageProtocol 프로젝트와의 컴파일 타임 의존을 끊는 지점(이름 문자열이 유일한 계약).
/// </summary>
internal sealed class AttributeReferences
{
    public const string RemoteProcedureTypeName = "DRPC.RemoteProcedure";
    public const string RpcDeliveryModeTypeName = "DRPC.RpcDeliveryMode";
    public const string ClientHubTypeName = "DRPC.Client.Network.ClientHub";
    public const string ServerHubTypeName = "DRPC.Server.Network.ServerHub";
    public const string ServerDeclarationsTypeName = "DRPC.Shared.Interface.IServerProcedureDeclarations";
    public const string ClientDeclarationsTypeName = "DRPC.Shared.Interface.IClientProcedureDeclarations";
    public const string MessageSerializableTypeName = "MessageProtocol.Serialize.IMessageSerializable";

    /// <summary>MessageProtocol 의 메시지 표시 속성류 (StandaloneMessage, NonIdMessage, GroupRoot/Element, Generic).</summary>
    public const string MessageNamespace = "MessageProtocol";

    public INamedTypeSymbol? RemoteProcedureAttributeType { get; }
    public INamedTypeSymbol? RpcDeliveryModeType { get; }
    public INamedTypeSymbol? MessageSerializableType { get; }

    public AttributeReferences(Compilation compilation)
    {
        RemoteProcedureAttributeType = compilation.GetTypeByMetadataName(RemoteProcedureTypeName);
        RpcDeliveryModeType = compilation.GetTypeByMetadataName(RpcDeliveryModeTypeName);
        MessageSerializableType = compilation.GetTypeByMetadataName(MessageSerializableTypeName + "`1");
    }

    public bool HasMessageAttribute(ITypeSymbol type)
        => MessageStyleOf(type) != MessageStyle.None;

    /// <summary>
    /// 메시지 타입의 직렬화 스타일. 와이어에 ID 헤더를 얹는 종류(Standalone/Group/Generic)와
    /// 헤더 없이 타입 고정으로 얹는 NonId 를 가른다 — 중첩 값을 쓸 때 어느 API 를 써야 하는지의 근거.
    /// </summary>
    public MessageStyle MessageStyleOf(ITypeSymbol type)
    {
        var style = MessageStyle.None;
        foreach (var attribute in type.GetAttributes())
        {
            string? name = attribute.AttributeClass?.Name;
            string? ns = attribute.AttributeClass?.ContainingNamespace?.ToDisplayString();
            if (ns != MessageNamespace || name == null)
            {
                continue;
            }

            switch (name)
            {
                case "NonIdMessageAttribute":
                    return MessageStyle.NonId;
                case "StandaloneMessageAttribute":
                case "GroupRootMessageAttribute":
                case "GroupElementMessageAttribute":
                case "GenericMessageAttribute":
                    style = MessageStyle.HasId;
                    break;
            }
        }

        if (style == MessageStyle.None && ImplementsMessageSerializable(type))
        {
            return MessageStyle.NonId;
        }

        return style;
    }

    public bool ImplementsMessageSerializable(ITypeSymbol type)
    {
        if (MessageSerializableType == null)
        {
            return false;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, MessageSerializableType))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>메시지 타입의 페이로드 기록 방식 구분.</summary>
internal enum MessageStyle
{
    /// <summary>MessageProtocol 메시지가 아님.</summary>
    None,

    /// <summary><c>[NonIdMessage]</c>(또는 타입 고정 직렬화) — 생성된 정적 Serialize/Deserialize 로 왕복한다.</summary>
    NonId,

    /// <summary>Standalone/Group/Generic — 헤더의 ID 로 라우팅하므로 object dispatch 가 가능하다(그룹 다형성 유지).</summary>
    HasId,
}
