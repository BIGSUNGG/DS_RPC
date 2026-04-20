using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator.Reference;

internal class AttributeReferences
{
    public INamedTypeSymbol? RemoteProcedureAttributeType { get; set; }
    public INamedTypeSymbol? ReliableTypeEnumType { get; set; }

    public AttributeReferences(Compilation compilation)
    {
        RemoteProcedureAttributeType = compilation.GetTypeByMetadataName("RPC.Attribute.RemoteProcedure");
        ReliableTypeEnumType = compilation.GetTypeByMetadataName("Communication.Network.RUDP.Shared.Messages.ReliableType");
    }
}
