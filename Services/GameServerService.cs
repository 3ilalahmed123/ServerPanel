using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace site.Services
{
    public class GameServerService
    {
        private readonly ILogger<GameServerService> _logger;
        private static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

        public GameServerService(ILogger<GameServerService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GetServerDetailsAsync(string profileName, string userName)
        {
            string executablePath = $"/home/{profileName}/{profileName}";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"-u {userName} {executablePath} details",
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
                    _logger.LogWarning($"⚠️ stderr: {error}");

                output = AnsiRegex.Replace(output, string.Empty);
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to get server details for {profileName}");
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<string> RunCommandAsync(string profileName, string userName, string args)
        {
            string executablePath = $"/home/{profileName}/{profileName}";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"-u {userName} {executablePath} {args}",
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
                _logger.LogError(ex, $"❌ Failed to run command '{args}' for {profileName}");
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<bool> IsServerStartedAsync(string profileName, string userName)
        {
            var details = await GetServerDetailsAsync(profileName, userName);
            return details.Contains("STARTED", StringComparison.OrdinalIgnoreCase);
        }
    }
}
