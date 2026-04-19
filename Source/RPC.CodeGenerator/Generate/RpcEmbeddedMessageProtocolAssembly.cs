using System.IO;
using System.Reflection;

namespace RPC.CodeGenerator.Generate;

/// <summary>
/// 분석기 호스트는 RPC.CodeGenerator.dll 만 로드하는 경우가 많아 MessageProtocol.CodeGenerator 임베드를 로드합니다.
/// </summary>
internal static class RpcEmbeddedMessageProtocolAssembly
{
    private static readonly Lazy<Assembly> Lazy = new(LoadEmbedded);

    internal static Assembly Assembly => Lazy.Value;

    internal static Type? SerializationEmitterFacadeType =>
        Assembly.GetType("MessageProtocol.CodeGenerator.Generate.SerializationEmitterFacade");

    internal static Type? MessageCodeGeneratorType =>
        Assembly.GetType("MessageProtocol.CodeGenerator.Generate.MessageCodeGenerator");

    static Assembly LoadEmbedded()
    {
        var executing = typeof(RpcEmbeddedMessageProtocolAssembly).Assembly;
        const string resourceName = "MessageProtocol.CodeGenerator.Embedded.dll";
#pragma warning disable RS1035 // 임베드된 분석기 전용
        using var stream = executing.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' was not found in '{executing.FullName}'.");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Assembly.Load(ms.ToArray());
#pragma warning restore RS1035
    }
}
