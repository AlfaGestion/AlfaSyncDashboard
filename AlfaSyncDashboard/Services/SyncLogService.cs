using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

public sealed class SyncLogService
{
    private readonly AppSettings _settings;

    public SyncLogService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task EnsureTableAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
IF OBJECT_ID('dbo.LOG_SYNC', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LOG_SYNC
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Fecha DATETIME NOT NULL DEFAULT GETDATE(),
        Local NVARCHAR(100) NOT NULL,
        Proceso NVARCHAR(100) NOT NULL,
        Mensaje NVARCHAR(MAX) NULL,
        Estado NVARCHAR(20) NOT NULL
    );
END";
        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = _settings.CommandTimeoutSeconds };
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task WriteAsync(string local, string proceso, string mensaje, string estado, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO dbo.LOG_SYNC(Local, Proceso, Mensaje, Estado)
VALUES (@Local, @Proceso, @Mensaje, @Estado);";
        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@Local", local);
        cmd.Parameters.AddWithValue("@Proceso", proceso);
        cmd.Parameters.AddWithValue("@Mensaje", mensaje);
        cmd.Parameters.AddWithValue("@Estado", estado);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<SyncLogEntry>> GetRecentAsync(int days = 30, int maxRows = 2000, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP (@TopRows) Fecha, Local, Proceso, Mensaje, Estado
FROM dbo.LOG_SYNC
WHERE Fecha >= DATEADD(DAY, -@Days, GETDATE())
ORDER BY Fecha DESC, Id DESC;";

        var result = new List<SyncLogEntry>();
        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@TopRows", maxRows);
        cmd.Parameters.AddWithValue("@Days", days);

        await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await dr.ReadAsync(cancellationToken))
        {
            result.Add(new SyncLogEntry
            {
                Fecha = dr.GetDateTime(dr.GetOrdinal("Fecha")),
                Local = dr["Local"]?.ToString()?.Trim() ?? string.Empty,
                Proceso = dr["Proceso"]?.ToString()?.Trim() ?? string.Empty,
                Estado = dr["Estado"]?.ToString()?.Trim() ?? string.Empty,
                Mensaje = dr["Mensaje"]?.ToString()?.Trim() ?? string.Empty
            });
        }

        return result;
    }
}
