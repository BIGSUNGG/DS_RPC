using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Communication.Network.RUDP;   // RudpTlsOptions — 인증서 지문 출력
using DRPC.Shared.Network;
using Sandbox.Contracts;
using Sandbox.Server;

const int Port = 9050;
const string ConnectionKey = "sandbox-key";

// --tls: 자가서명 인증서를 만들어 DTLS 1.2 로 패킷을 암호화한다. 클라이언트는 출력된 지문으로 핀닝 검증한다.
bool tls = args.Contains("--tls");

var serverOptions = new RpcEndpointOptions { ConnectionKey = ConnectionKey };
if (tls)
{
    X509Certificate2 certificate = CreateSelfSignedCertificate();
    serverOptions.ServerCertificate = certificate;
    Console.WriteLine($"[server] DTLS 1.2 ON — 인증서 지문(SHA-256): {RudpTlsOptions.GetSha256Fingerprint(certificate.Export(X509ContentType.Cert))}");
    Console.WriteLine("[server] 클라이언트 실행: dotnet run --project Sandbox/Sandbox.Client -- --tls <지문>");
}

await using var handle = await GameServerHub.ListenAsync(Port, serverOptions, async hub =>
{
    Console.WriteLine("[server] client connected — 서버가 클라이언트로 역호출 시작");

    // 클라이언트 계약(IGameClientProcedures)의 outgoing 스텁: 그대로 await 하면 된다.
    float sum = await hub.EchoSumAsync(new List<float> { 1.5f, 2.25f, 4f });
    Console.WriteLine($"[server] EchoSum -> {sum}");

    int count = await hub.CountConfigAsync("arena", new[] { 1, 2, 3 });
    Console.WriteLine($"[server] CountConfig -> {count}");

    // OneWay 은 응답 없이 보낸다.
    await hub.NotifyScoreAsync(new ScoreBoard
    {
        Map = "arena",
        Lines = { new ScoreLine { PlayerId = 7, Score = 42 } },
    });
    Console.WriteLine("[server] NotifyScore(one-way) sent");
});

Console.WriteLine($"[server] RUDP 리스너 가동 중 (127.0.0.1:{Port}, key={ConnectionKey}{(tls ? ", DTLS ON" : "")})");
Console.WriteLine("[server] 중지하려면 ENTER.");
Console.ReadLine();

static X509Certificate2 CreateSelfSignedCertificate()
{
    using RSA rsa = RSA.Create(2048);
    CertificateRequest request = new("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
        new OidCollection { new("1.3.6.1.5.5.7.3.1") }, critical: false));
    using X509Certificate2 ephemeral = request.CreateSelfSigned(
        DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30));
    // 플랫폼 키 저장소와 무관하게 소유 키가 동작하도록 PFX 로 재수입한다(테스트·Communication 패턴 동일).
    return new X509Certificate2(
        ephemeral.Export(X509ContentType.Pfx),
        (string?)null,
        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
}
