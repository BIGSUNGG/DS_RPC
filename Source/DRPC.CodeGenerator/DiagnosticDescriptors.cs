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
        title: "DRPC hub must inherit ClientHub or ServerHub",
        messageFormat: "The type '{0}' must inherit ClientHub<TSPD, TCPD> (DRPC.Client.Network) or ServerHub<TSPD, TCPD> (DRPC.Server.Network).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "DRPCGEN003",
        title: "DRPC contract type not supported",
        messageFormat: "Method '{0}' uses unsupported type '{1}'. Use primitives, string, enums, nullable primitives, byte[], arrays/List<T> of those, or a MessageProtocol message type. Reason: {2}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingExplicitMethodId = new(
        id: "DRPCGEN004",
        title: "DRPC method should declare an explicit MethodId",
        messageFormat: "Method '{0}' relies on declaration-order MethodId {1}. Pass an explicit methodId to [RemoteProcedure] for a stable contract.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateMethodId = new(
        id: "DRPCGEN005",
        title: "DRPC MethodId is duplicated",
        messageFormat: "MethodId {0} is used more than once in declaration '{1}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OneWayRequiresVoid = new(
        id: "DRPCGEN006",
        title: "DRPC OneWay requires void return",
        messageFormat: "Method '{0}' sets OneWay=true but does not return void.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
