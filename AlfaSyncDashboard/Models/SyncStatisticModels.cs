namespace AlfaSyncDashboard.Models;

public sealed class SyncStatisticPayload
{
    public string IdCliente { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public int Secuencia { get; set; }
    public string NombrePC { get; set; } = string.Empty;
    public string ServidorSQL { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public int NroError { get; set; }
    public string MensajeError { get; set; } = string.Empty;
    public string Proceso { get; set; } = string.Empty;
    public string FhHsFinProceso { get; set; } = string.Empty;
    public string Archivo { get; set; } = string.Empty;
}
