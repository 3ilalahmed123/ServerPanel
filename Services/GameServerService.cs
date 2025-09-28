using QueryMaster;
using QueryMaster.GameServer;
using Microsoft.Extensions.Logging;

namespace site.Services;

public class GameServerService
{
    private readonly ILogger<GameServerService> _logger;
    private readonly string[] _ipCandidates;
    private readonly ushort _port;

    public GameServerService(ILogger<GameServerService> logger)
    {
        _logger = logger;

        // Try localhost first, then public IP
        var publicIp = Environment.GetEnvironmentVariable("GAMESERVER_PUBLIC_IP") ?? "51.89.166.121";
        _ipCandidates = new[] { "127.0.0.1", publicIp };

        _port = ushort.TryParse(Environment.GetEnvironmentVariable("GAMESERVER_PORT"), out var p)
            ? p
            : (ushort)27015;

        _logger.LogInformation($"🎯 GameServerService initialized. IP candidates: {string.Join(", ", _ipCandidates)} Port: {_port}");
    }

    public async Task<ServerInfo?> GetInfoAsync()
    {
        foreach (var ip in _ipCandidates)
        {
            try
            {
                _logger.LogInformation($"🔄 Querying server info at {ip}:{_port}");
                using var server = ServerQuery.GetServerInstance(
                    EngineType.Source,
                    ip,
                    _port,
                    sendTimeout: 2000,
                    receiveTimeout: 2000
                );

                if (server == null)
                {
                    _logger.LogWarning($"⚠️ Could not create QueryMaster instance for {ip}");
                    continue;
                }

                var info = await Task.Run(() => server.GetInfo());

                if (info != null)
                {
                    _logger.LogInformation($"✅ Server online at {ip}:{_port} | Name: {info.Name} | Map: {info.Map} | Players: {info.Players}/{info.MaxPlayers}");
                    return info;
                }

                _logger.LogWarning($"⚠️ server.GetInfo() returned null from {ip}:{_port}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error while querying {ip}:{_port}");
            }
        }

        _logger.LogError("🚨 All IP candidates failed. Could not get server info.");
        return null;
    }

    public async Task<PlayerInfo[]> GetPlayersAsync()
    {
        foreach (var ip in _ipCandidates)
        {
            try
            {
                _logger.LogInformation($"🔄 Querying player list at {ip}:{_port}");
                using var server = ServerQuery.GetServerInstance(
                    EngineType.Source,
                    ip,
                    _port,
                    sendTimeout: 2000,
                    receiveTimeout: 2000
                );

                if (server == null)
                {
                    _logger.LogWarning($"⚠️ Could not create QueryMaster instance for {ip}");
                    continue;
                }

                var players = await Task.Run(() => server.GetPlayers()?.ToArray() ?? Array.Empty<PlayerInfo>());
                _logger.LogInformation($"📊 Retrieved {players.Length} players from {ip}.");
                return players;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error while querying players from {ip}:{_port}");
            }
        }

        return Array.Empty<PlayerInfo>();
    }
}
