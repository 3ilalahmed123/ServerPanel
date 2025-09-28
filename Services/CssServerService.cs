using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace site.Services;

public class CssServerService
{
    private readonly ILogger<CssServerService> _logger;

    public CssServerService(ILogger<CssServerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetServerDetailsAsync()
    {
        try
        {
            _logger.LogInformation("🔧 Running ./cssserver details as cssserver user...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = "-c \"sudo -u cssserver ./cssserver details\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = "/home/cssserver", // where ./cssserver lives
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning($"⚠️ CssServerService stderr: {error}");
            }

            _logger.LogInformation("✅ cssserver details executed successfully.");
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to execute cssserver details.");
            return $"Error: {ex.Message}";
        }
    }
}
