using System.Diagnostics;
using System.Text;
using AlfaSyncDashboard.Models;

namespace AlfaSyncDashboard.Services;

public sealed class ScheduledTaskService
{
    public const string TaskName = "AlfaSyncDashboard AutoSync";

    public string BuildCreateCommand(int intervalMinutes, SyncExecutionMode mode)
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, "AlfaSyncDashboard.exe");
        var syncArg = mode == SyncExecutionMode.Full ? "full" : "prices";
        var taskRun = $"\"{exePath}\" --sync {syncArg}";

        return $"schtasks /Create /F /SC MINUTE /MO {intervalMinutes} /TN \"{TaskName}\" /TR \"{taskRun}\" /RU SYSTEM";
    }

    public string BuildDeleteCommand()
        => $"schtasks /Delete /F /TN \"{TaskName}\"";

    public async Task<string> CreateOrUpdateAsync(int intervalMinutes, SyncExecutionMode mode, CancellationToken cancellationToken = default)
    {
        var arguments = BuildCommandArguments("/Create", intervalMinutes, mode);
        return await RunSchtasksAsync(arguments, cancellationToken);
    }

    public async Task<string> DeleteAsync(CancellationToken cancellationToken = default)
        => await RunSchtasksAsync($"/Delete /F /TN \"{TaskName}\"", cancellationToken);

    public async Task<string> QueryAsync(CancellationToken cancellationToken = default)
        => await RunSchtasksAsync($"/Query /TN \"{TaskName}\" /V /FO LIST", cancellationToken);

    private static string BuildCommandArguments(string operation, int intervalMinutes, SyncExecutionMode mode)
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, "AlfaSyncDashboard.exe");
        var syncArg = mode == SyncExecutionMode.Full ? "full" : "prices";
        var taskRun = $"\\\"{exePath}\\\" --sync {syncArg}";

        var builder = new StringBuilder();
        builder.Append(operation);
        builder.Append(" /F /SC MINUTE");
        builder.Append($" /MO {intervalMinutes}");
        builder.Append($" /TN \"{TaskName}\"");
        builder.Append($" /TR \"{taskRun}\"");
        builder.Append(" /RU SYSTEM");
        return builder.ToString();
    }

    private static async Task<string> RunSchtasksAsync(string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var output = string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}{Environment.NewLine}{stderr}";
        if (process.ExitCode != 0)
            throw new InvalidOperationException(output.Trim());

        return output.Trim();
    }
}
