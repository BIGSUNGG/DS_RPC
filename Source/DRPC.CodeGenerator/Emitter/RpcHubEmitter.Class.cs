using System.Text;
using DRPC.CodeGenerator.Metadata;

namespace DRPC.CodeGenerator.Emitter;

internal static partial class RpcHubEmitter
{
    static void EmitPartialClass(StringBuilder sb, TypeMetadata typeMeta, string indent)
    {
        string hub = typeMeta.Symbol.Name;

        sb.AppendLine($"{indent}public partial class {hub}");
        sb.AppendLine($"{indent}{{");

        EmitConstructors(sb, typeMeta, indent + "    ");

        if (typeMeta.IsServerEndpoint)
        {
            EmitListenAsync(sb, hub, indent + "    ");
        }
        else
        {
            EmitConnectAsync(sb, hub, indent + "    ");
        }

        EmitRpcMessageTypes(sb, typeMeta, indent + "    ");

        foreach (var proc in typeMeta.Outgoing)
        {
            EmitOutgoingProcedure(sb, proc, indent + "    ");
        }

        foreach (var proc in typeMeta.Incoming)
        {
            EmitIncomingProcedure(sb, proc, indent + "    ");
        }

        EmitDefaultMessageConverter(sb, indent + "    ");

        if (typeMeta.NeedsStringHelpers)
        {
            EmitStringHelpers(sb, indent + "    ");
        }

        sb.AppendLine($"{indent}}}");
    }

    static void EmitConstructors(StringBuilder sb, TypeMetadata model, string indent)
    {
        string hub = model.Symbol.Name;
        sb.AppendLine($"{indent}public {hub}(global::System.Func<HubBase, ISession> sessionFactory)");
        sb.AppendLine($"{indent}    : base(sessionFactory)");
        sb.AppendLine($"{indent}{{");
        foreach (var proc in model.Incoming)
        {
            sb.AppendLine($"{indent}    MethodCallActions.Add({proc.MethodId}, {proc.MethodName}_Requested);");
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }
}
