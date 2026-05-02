using Microsoft.CodeAnalysis;

namespace DRPC.CodeGenerator;

internal static class DiagnosticDescriptors
{
    const string Category = "DRPC";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "DRPCGEN001",
        title: "DRPC hub type must be partial",
        messageFormat: "The DRPC hub type '{0}' must be declared partial.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidHubBase = new(
        id: "DRPCGEN002",
        title: "DRPC hub must inherit ServerHub or ClientHub",
        messageFormat: "The type '{0}' must inherit DRPC.Client.Network.ServerHub<,> or DRPC.Server.Netwrok.ClientHub<,>.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "DRPCGEN003",
        title: "DRPC type not supported",
        messageFormat: "Method '{0}' uses unsupported type '{1}' for DRPC generation. Use void, primitives, string, or enumerable types.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
