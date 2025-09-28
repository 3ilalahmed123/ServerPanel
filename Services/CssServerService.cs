using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace site.Services;

public class CssServerService
{
    private readonly ILogger<CssServerService> _logger;
    private readonly string _scriptPath;

    public CssServerService(ILogger<CssServerService> logger)
    {
        _logger = logger;
        _scriptPath = "/home/cssserver/cssserver"; // Path to your CSS LinuxGSM script
    }

    public async Task<string> GetServerDetailsAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sudo",
                ArgumentList = { "-u", "cssserver", _scriptPath, "details" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("🔧 Running LinuxGSM CSS details as cssserver...");

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("❌ Failed to start cssserver process.");
                return "Error: process could not start.";
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("⚠️ cssserver reported errors: {Error}", error);
            }

            _logger.LogInformation("✅ cssserver details retrieved successfully.");
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error while retrieving cssserver details");
            return $"Error: {ex.Message}";
        }
    }
}
