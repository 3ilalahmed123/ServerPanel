using QueryMaster;
using QueryMaster.GameServer;
using Microsoft.Extensions.Logging;

namespace site.Services;

public class GameServerService
{
    private readonly ILogger<GameServerService> _logger;
    private readonly string _ip;
    private readonly ushort _port;

    public GameServerService(ILogger<GameServerService> logger)
    {
        _logger = logger;

        // 🔧 Prefer localhost (since dotnet app & SRCDS are on same VPS)
        _ip = Environment.GetEnvironmentVariable("GAMESERVER_IP") ?? "127.0.0.1";
        _port = ushort.TryParse(Environment.GetEnvironmentVariable("GAMESERVER_PORT"), out var p)
            ? p
            : (ushort)27015;

        _logger.LogInformation($"🎯 GameServerService initialized with target {_ip}:{_port}");
    }

    public async Task<ServerInfo?> GetInfoAsync()
    {
        try
        {
            _logger.LogInformation($"🔄 Querying server info at {_ip}:{_port}");
            using var server = ServerQuery.GetServerInstance(
                EngineType.Source,
                _ip,
                _port,
                sendTimeout: 2000,
                receiveTimeout: 2000
            );

            if (server == null)
            {
                _logger.LogWarning("⚠️ Failed to create server instance — connection not possible.");
                return null;
            }

            var info = await Task.Run(() => server.GetInfo());

            if (info == null)
                _logger.LogWarning("⚠️ server.GetInfo() returned null (server may be offline or blocked)");
            else
                _logger.LogInformation($"✅ Server responded: {info.Name} | Map: {info.Map} | Players: {info.Players}/{info.MaxPlayers}");

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error while querying {_ip}:{_port}");
            return null;
        }
    }

    public async Task<PlayerInfo[]> GetPlayersAsync()
    {
        try
        {
            _logger.LogInformation($"🔄 Querying player list at {_ip}:{_port}");
            using var server = ServerQuery.GetServerInstance(
                EngineType.Source,
                _ip,
                _port,
                sendTimeout: 2000,
                receiveTimeout: 2000
            );

            if (server == null)
            {
                _logger.LogWarning("⚠️ Failed to create server instance — cannot fetch players.");
                return Array.Empty<PlayerInfo>();
            }

            var players = await Task.Run(() => server.GetPlayers()?.ToArray() ?? Array.Empty<PlayerInfo>());
            _logger.LogInformation($"📊 Retrieved {players.Length} players.");
            return players;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error while querying players from {_ip}:{_port}");
            return Array.Empty<PlayerInfo>();
        }
    }
}
