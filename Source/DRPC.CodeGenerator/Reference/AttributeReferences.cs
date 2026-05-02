using Microsoft.CodeAnalysis;

namespace DRPC.CodeGenerator.Reference;

internal class AttributeReferences
{
    public INamedTypeSymbol? RemoteProcedureAttributeType { get; set; }
    public INamedTypeSymbol? ReliableTypeEnumType { get; set; }
    public INamedTypeSymbol? MessageAttributeType { get; set; }
    public INamedTypeSymbol? NonIdMessageAttributeType { get; set; }
    public INamedTypeSymbol? GroupRootMessageAttributeType { get; set; }
    public INamedTypeSymbol? GroupElementMessageAttributeType { get; set; }
    public INamedTypeSymbol? StandaloneMessageAttributeType { get; set; }

    public AttributeReferences(Compilation compilation)
    {
        RemoteProcedureAttributeType = compilation.GetTypeByMetadataName("DRPC.Attribute.RemoteProcedure");
        ReliableTypeEnumType = compilation.GetTypeByMetadataName("Communication.Network.RUDP.Shared.Messages.ReliableType");
        MessageAttributeType = compilation.GetTypeByMetadataName("MessageProtocol.MessageAttribute");
        NonIdMessageAttributeType = compilation.GetTypeByMetadataName("MessageProtocol.NonIdMessageAttribute");
        GroupRootMessageAttributeType = compilation.GetTypeByMetadataName("MessageProtocol.GroupRootMessageAttribute");
        GroupElementMessageAttributeType = compilation.GetTypeByMetadataName("MessageProtocol.GroupElementMessageAttribute");
        StandaloneMessageAttributeType = compilation.GetTypeByMetadataName("MessageProtocol.StandaloneMessageAttribute");
    }
}
