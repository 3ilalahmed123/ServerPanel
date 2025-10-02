using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace site.Services;

public class GameConfigService
{
    private readonly ILogger<GameConfigService> _logger;

    public GameConfigService(ILogger<GameConfigService> logger)
    {
        _logger = logger;
    }

    // Allowed editable files for a given profile (top 3)
    public IReadOnlyList<string> GetEditableFiles(string profileName)
    {
        return new[]
        {
        "_default.cfg",
        "common.cfg",
        $"{profileName}.cfg",      // LinuxGSM instance config
        $"{profileName}-server.cfg", // serverfiles cfg (alias)
        "mapcycle.txt"
    };
    }



    private static string BuildConfigPath(string profileName, string fileName)
    {
        // LinuxGSM cfg
        if (string.Equals(fileName, $"{profileName}.cfg", StringComparison.OrdinalIgnoreCase))
            return $"/home/{profileName}/lgsm/config-lgsm/{profileName}/{fileName}";

        // Server gameplay config
        if (string.Equals(fileName, $"{profileName}-server.cfg", StringComparison.OrdinalIgnoreCase))
            return $"/home/{profileName}/serverfiles/cstrike/cfg/{profileName}.cfg";

        // Mapcycle.txt
        if (string.Equals(fileName, "mapcycle.txt", StringComparison.OrdinalIgnoreCase))
            return $"/home/{profileName}/serverfiles/cstrike/cfg/mapcycle.txt";

        throw new InvalidOperationException("File not allowed.");
    }








    public async Task<string> ReadConfigAsync(string profileName, string userName, string fileName)
    {
        var allowed = GetEditableFiles(profileName);
        if (!allowed.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("File not allowed.");

        var path = BuildConfigPath(profileName, fileName);

        var psi = new ProcessStartInfo
        {
            FileName = "sudo",
            Arguments = $"-u {userName} cat {path}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };
        p.Start();
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
        {
            _logger.LogWarning("ReadConfig stderr: {Stderr}", stderr);
            throw new InvalidOperationException($"Failed to read config: {stderr}");
        }

        return stdout;
    }

    // Writes atomically using `tee` as the target user (no quoting issues, full content via STDIN)
    public async Task WriteConfigAsync(string profileName, string userName, string fileName, string content)
    {
        var allowed = GetEditableFiles(profileName);
        if (!allowed.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("File not allowed.");

        var path = BuildConfigPath(profileName, fileName);

        var psi = new ProcessStartInfo
        {
            FileName = "sudo",
            Arguments = $"-u {userName} tee {path}",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };
        p.Start();

        await p.StandardInput.WriteAsync(content);
        p.StandardInput.Close();

        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
        {
            _logger.LogWarning("WriteConfig stderr: {Stderr}", stderr);
            throw new InvalidOperationException($"Failed to write config: {stderr}");
        }
    }

}
