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
        $"{profileName}.cfg",
        "mapcycle.txt",
        "cssserver.cfg"
    };
    }


    private static string BuildConfigPath(string profileName, string fileName)
    {
        // Mapcycle.txt special case
        if (string.Equals(fileName, "mapcycle.txt", StringComparison.OrdinalIgnoreCase))
        {
            var path = $"/home/{profileName}/serverfiles/cstrike/mapcycle.txt";

            // If missing, copy from default
            if (!File.Exists(path))
            {
                var defaultPath = $"/home/{profileName}/serverfiles/cstrike/cfg/mapcycle_default.txt";
                if (File.Exists(defaultPath))
                    File.Copy(defaultPath, path);
                else
                    File.WriteAllText(path, string.Empty);
            }

            return path;
        }


        // CSS and TF2 server.cfg special case
        if (string.Equals(fileName, "cssserver.cfg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "server.cfg", StringComparison.OrdinalIgnoreCase))
            return $"/home/{profileName}/serverfiles/cstrike/cfg/{fileName}";

        // Default LGSM config .cfg files
        return $"/home/{profileName}/lgsm/config-lgsm/{profileName}/{fileName}";
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
