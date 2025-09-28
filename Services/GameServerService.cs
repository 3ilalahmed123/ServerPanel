using QueryMaster;
using QueryMaster.GameServer;
using Microsoft.Extensions.Logging;
using System.Net;

namespace site.Services;

public class GameServerService
{
    private readonly ILogger<GameServerService> _logger;
    private readonly string[] _ipCandidates;
    private readonly ushort _port;

    public GameServerService(ILogger<GameServerService> logger)
    {
        _logger = logger;

        // 🏠 Prefer localhost first, fallback to environment or public IP
        var localhost = "127.0.0.1";
        var envIp = Environment.GetEnvironmentVariable("GAMESERVER_IP");
        var publicIp = Environment.GetEnvironmentVariable("INTERNET_IP") ?? GetPublicIp();

        _ipCandidates = new[] { localhost, envIp, publicIp }
            .Where(ip => !string.IsNullOrWhiteSpace(ip))
            .Distinct()
            .ToArray();

        _port = ushort.TryParse(Environment.GetEnvironmentVariable("GAMESERVER_PORT"), out var p)
            ? p
            : (ushort)27015;

        _logger.LogInformation($"🎯 GameServerService initialized with candidates: {string.Join(", ", _ipCandidates)} (Port: {_port})");
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
                    _logger.LogWarning($"⚠️ Failed to create server instance for {ip}:{_port}");
                    continue;
                }

                var info = await Task.Run(() => server.GetInfo());
                if (info != null)
                {
                    _logger.LogInformation($"✅ Server responded via {ip}: {info.Name} | Map: {info.Map} | Players: {info.Players}/{info.MaxPlayers}");
                    return info;
                }

                _logger.LogWarning($"⚠️ server.GetInfo() returned null from {ip}:{_port}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"❌ Query failed for {ip}:{_port}");
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
                    _logger.LogWarning($"⚠️ Failed to create server instance for {ip}:{_port}");
                    continue;
                }

                var players = await Task.Run(() => server.GetPlayers()?.ToArray() ?? Array.Empty<PlayerInfo>());
                _logger.LogInformation($"📊 Retrieved {players.Length} players from {ip}.");
                return players;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"❌ Player query failed for {ip}:{_port}");
            }
        }

        _logger.LogError("🚨 All IP candidates failed. Returning empty player list.");
        return Array.Empty<PlayerInfo>();
    }

    private static string GetPublicIp()
    {
        try
        {
            // Best-effort attempt to get VPS public IP
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            return client.GetStringAsync("https://api.ipify.org").Result;
        }
        catch
        {
            return "51.89.166.121"; // fallback to hardcoded known IP
        }
    }
}
