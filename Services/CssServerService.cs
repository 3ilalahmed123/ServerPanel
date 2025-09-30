using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace site.Services;

public class CssServerService
{
    private readonly ILogger<CssServerService> _logger;
    private static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    public CssServerService(ILogger<CssServerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetServerDetailsAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = "-u cssserver /home/cssserver/cssserver details",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
                _logger.LogWarning($"⚠️ cssserver stderr: {error}");

            // 🧹 Remove ANSI colors so you just get readable text
            output = AnsiRegex.Replace(output, string.Empty);

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to run cssserver details");
            return $"ERROR: {ex.Message}";
        }
    }

    public async Task<string> RunCommandAsync(string args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = $"-u cssserver /home/cssserver/cssserver {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            output = AnsiRegex.Replace(output, string.Empty);
            if (!string.IsNullOrEmpty(error))
                output += $"\n⚠️ stderr: {error}";

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to run command: {args}");
            return $"ERROR: {ex.Message}";
        }
    }

    public async Task<bool> IsServerStartedAsync()
    {
        var details = await GetServerDetailsAsync();
        return details.Contains("STARTED", StringComparison.OrdinalIgnoreCase);
    }
}
