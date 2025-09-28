using QueryMaster;
using QueryMaster.GameServer;

namespace site.Services;

public class GameServerService
{
    private readonly string _ip;
    private readonly ushort _port;

    public GameServerService(string ip, ushort port)
    {
        _ip = ip;
        _port = port;
    }

    public async Task<ServerInfo?> GetInfoAsync()
    {
        try
        {
            using var server = ServerQuery.GetServerInstance(
                EngineType.Source,
                _ip,
                _port,
                sendTimeout: 2000,
                receiveTimeout: 2000
            );

            return server?.GetInfo();
        }
        catch
        {
            return null;
        }
    }

    public async Task<PlayerInfo[]> GetPlayersAsync()
    {
        try
        {
            using var server = ServerQuery.GetServerInstance(
                EngineType.Source,
                _ip,
                _port,
                sendTimeout: 2000,
                receiveTimeout: 2000
            );

            return server?.GetPlayers().ToArray() ?? Array.Empty<PlayerInfo>();
        }
        catch
        {
            return Array.Empty<PlayerInfo>();
        }
    }
}
