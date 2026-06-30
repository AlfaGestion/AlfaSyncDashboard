namespace AlfaSyncDashboard.Services;

public sealed class LocalFileLogService
{
    private readonly string _logFilePath;
    private readonly object _sync = new();

    public LocalFileLogService()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "AlfaSyncDashboard",
            "Logs");

        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"sync-{DateTime.Now:yyyyMMdd}.log");
    }

    public string LogFilePath => _logFilePath;

    public void Write(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        lock (_sync)
        {
            File.AppendAllText(_logFilePath, line);
        }
    }
}
