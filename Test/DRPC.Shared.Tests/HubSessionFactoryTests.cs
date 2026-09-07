using Communication.Network.RUDP;
using DRPC.Shared.Network;
using Xunit;

namespace DRPC.Shared.Tests;

/// <summary>
/// HubSessionFactory.CreateTransportOptions 계약: 접속 키 전달·연결 타임아웃 상한 매핑(Communication 2.0.1 ConnectTimeout 채택).
/// </summary>
public class HubSessionFactoryTests
{
    [Fact]
    public void Transport_options_default_keeps_key_and_timeout_unset()
    {
        var options = HubSessionFactory.CreateTransportOptions(null);

        Assert.Equal(RudpTransportOptions.DefaultConnectionKey, options.ConnectionKey);
        Assert.Null(options.ConnectTimeout); // 전송 스택 기본(약 5초) 유지
    }

    [Fact]
    public void Transport_options_applies_nonempty_connection_key()
    {
        var options = HubSessionFactory.CreateTransportOptions("game-key");

        Assert.Equal("game-key", options.ConnectionKey);
    }

    [Theory]
    [InlineData(0)]
    public void Transport_options_zero_timeout_keeps_transport_default(int connectTimeoutMs)
    {
        // 0 은 "미설정"(기본 매개변수값) — 전송 스택 기본(약 5초)을 유지한다.
        var options = HubSessionFactory.CreateTransportOptions(null, connectTimeoutMs);

        Assert.Null(options.ConnectTimeout);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Transport_options_negative_timeout_is_rejected(int connectTimeoutMs)
    {
        // 상한은 양수만 의미가 있다 — 음수는 계약 위반이므로 묵묵히 무시하지 않고 거부한다.
        Assert.Throws<ArgumentOutOfRangeException>("connectTimeoutMs",
            () => HubSessionFactory.CreateTransportOptions(null, connectTimeoutMs));
    }

    [Fact]
    public void Transport_options_positive_timeout_is_applied()
    {
        var options = HubSessionFactory.CreateTransportOptions("game-key", 1500);

        Assert.Equal("game-key", options.ConnectionKey);
        Assert.Equal(1500, options.ConnectTimeout);
    }

    [Fact]
    public void Transport_options_max_connections_maps_and_defaults()
    {
        // 기본·명시적 0 은 무제한(null) — 상한은 양수만 설정한다.
        Assert.Null(HubSessionFactory.CreateTransportOptions(null).MaxConnections);
        Assert.Null(HubSessionFactory.CreateTransportOptions(null, 0, 0).MaxConnections);
        Assert.Equal(1, HubSessionFactory.CreateTransportOptions(null, 0, 1).MaxConnections);
        Assert.Equal(64, HubSessionFactory.CreateTransportOptions("game-key", 0, 64).MaxConnections);
    }

    [Fact]
    public void Transport_options_negative_max_connections_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>("maxConnections",
            () => HubSessionFactory.CreateTransportOptions(null, 0, -1));
    }
}
