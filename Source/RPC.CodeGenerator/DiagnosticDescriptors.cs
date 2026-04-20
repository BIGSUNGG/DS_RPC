using Microsoft.CodeAnalysis;

namespace RPC.CodeGenerator;

internal static class DiagnosticDescriptors
{
    const string Category = "RPC";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "RPCGEN001",
        title: "RPC hub type must be partial",
        messageFormat: "The RPC hub type '{0}' must be declared partial.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidHubBase = new(
        id: "RPCGEN002",
        title: "RPC hub must inherit ServerHub or ClientHub",
        messageFormat: "The type '{0}' must inherit RPC.Client.Network.ServerHub<,> or RPC.Server.Netwrok.ClientHub<,>.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "RPCGEN003",
        title: "RPC type not supported",
        messageFormat: "Method '{0}' uses unsupported type '{1}' for RPC generation. Use void, primitives, string, or enumerable types.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
