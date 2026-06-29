namespace AlfaSyncDashboard.Services;

public sealed class ExecutionLockService
{
    private readonly string _lockFilePath;

    public ExecutionLockService()
    {
        var lockDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "AlfaSyncDashboard");

        Directory.CreateDirectory(lockDir);
        _lockFilePath = Path.Combine(lockDir, "sync.lock");
    }

    public bool TryAcquire(out IDisposable? handle, out string message)
    {
        try
        {
            var stream = new FileStream(_lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            handle = stream;
            message = string.Empty;
            return true;
        }
        catch (IOException)
        {
            handle = null;
            message = "Ya hay una sincronización en ejecución en este equipo.";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            handle = null;
            message = $"No se pudo obtener el bloqueo exclusivo en '{_lockFilePath}'.";
            return false;
        }
    }
}
