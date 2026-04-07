using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace RPC.Server.Netwrok;

public sealed class Listener
{
    private IPAddress _ipAddress;
    private int _port;
    private string _connectionKey;

    private readonly NetManager _netManager;
    private readonly EventBasedNetListener _listener;
    private Func<NetPeer, NetManager, EventBasedNetListener, Task>? _onClientAccepted;
    private CancellationToken _cancellationToken;

    public Listener(IPAddress ipAddress, int port, string connectionKey = "")
    {
        _ipAddress = ipAddress;
        _port = port;
        _connectionKey = connectionKey;

        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);  

        // 연결 요청 수락
        _listener.ConnectionRequestEvent += (request) =>
        {
            request.Accept();
        };

        // 클라이언트 연결 이벤트
        _listener.PeerConnectedEvent += (peer) =>
        {
            if (_onClientAccepted != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _onClientAccepted(peer, _netManager, _listener);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling client connection: {ex.Message}");
                    }
                }, _cancellationToken);
            }
        };      
    }


    public async Task ListenAsync(Func<NetPeer, NetManager, EventBasedNetListener, Task> onClientAccepted, CancellationToken token)
    {
        _netManager.Start(_port);
        _onClientAccepted = onClientAccepted;
        _cancellationToken = token;

        while (!token.IsCancellationRequested)
        {
            _netManager.PollEvents();
            await Task.Delay(15, token);
        }
    }
}
