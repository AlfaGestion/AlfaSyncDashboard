namespace AlfaSyncDashboard.Models;

public sealed class SyncLogEntry
{
    public DateTime Fecha { get; set; }
    public string Local { get; set; } = string.Empty;
    public string Proceso { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
