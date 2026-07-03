using System.Data;
using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

public sealed class LocalPendingSyncService
{
    private const string PendingStatus = "PENDIENTE";
    private const string ProcessingStatus = "PROCESANDO";
    private const string ProcessedStatus = "PROCESADO";
    private const string ErrorStatus = "ERROR";

    private readonly AppSettings _settings;

    public LocalPendingSyncService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<PendingSyncSummary> ProcessBeforePushAsync(TpvInfo tpv, Action<string> appendLog, CancellationToken cancellationToken)
    {
        await using var localConnection = new SqlConnection(tpv.BuildLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);

        if (!await SyncTableExistsAsync(localConnection, cancellationToken))
        {
            appendLog($"[{tpv.Descripcion}] No existe dbo.SYNC_PRECIOS_SERVER. Se continúa con la sincronización normal.");
            return PendingSyncSummary.TableMissing();
        }

        var summary = new PendingSyncSummary { TableExists = true };
        var blockedCount = await CountBlockedRowsAsync(localConnection, cancellationToken);
        if (blockedCount > 0)
            throw new InvalidOperationException($"Hay {blockedCount} registro(s) en estado {ProcessingStatus} o {ErrorStatus} en dbo.SYNC_PRECIOS_SERVER. Revisá los pendientes locales antes de enviar desde central.");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pending = await ClaimNextPendingAsync(localConnection, cancellationToken);
            if (pending is null)
                break;

            summary.PendingFound = true;
            appendLog($"[{tpv.Descripcion}] Pendiente local tomado: IdSync={pending.IdSync}, Artículo={pending.IdArticulo}, Lista={(pending.IdLista ?? "(null)")}, Usuario={pending.Usuario ?? "-"}.");

            try
            {
                var spec = pending.IdLista is null ? SyncMetadata.ArticlesSpec : SyncMetadata.PricesSpec;
                var sourceRow = await LoadSingleLocalRowAsync(localConnection, spec, pending, cancellationToken);
                if (sourceRow.Rows.Count == 0)
                    throw new InvalidOperationException($"No se encontró la fila actual en {spec.TargetObject} para el pendiente {pending.IdSync}.");

                await UpsertIntoCentralAsync(spec, sourceRow, cancellationToken);
                await MarkProcessedAsync(localConnection, pending.IdSync, cancellationToken);
                summary.ProcessedCount++;
                appendLog($"[{tpv.Descripcion}] Pendiente local procesado: IdSync={pending.IdSync} -> {spec.DisplayName} actualizado en central.");
            }
            catch (OperationCanceledException)
            {
                await RequeuePendingAsync(localConnection, pending.IdSync, "Proceso cancelado antes de completar la copia hacia central.", CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                await MarkErrorAsync(localConnection, pending.IdSync, ex.Message, CancellationToken.None);
                summary.ErrorCount++;
                appendLog($"[{tpv.Descripcion}] Pendiente local con error: IdSync={pending.IdSync}. {ex.Message}");
            }
        }

        blockedCount = await CountBlockedRowsAsync(localConnection, cancellationToken);
        if (blockedCount > 0)
            throw new InvalidOperationException($"La sincronización local previa dejó {blockedCount} registro(s) sin resolver en dbo.SYNC_PRECIOS_SERVER. Se cancela el envío desde central.");

        if (summary.ProcessedCount > 0)
            appendLog($"[{tpv.Descripcion}] Pendientes locales resueltos antes del envío normal. Procesados={summary.ProcessedCount}.");

        return summary;
    }

    private async Task<bool> SyncTableExistsAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT OBJECT_ID('dbo.SYNC_PRECIOS_SERVER', 'U');";
        await using var cmd = new SqlCommand(sql, localConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != DBNull.Value && result is not null;
    }

    private async Task<int> CountBlockedRowsAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
SELECT COUNT(*)
FROM dbo.SYNC_PRECIOS_SERVER
WHERE Estado IN (@processing, @error);
""";
        await using var cmd = new SqlCommand(sql, localConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@processing", ProcessingStatus);
        cmd.Parameters.AddWithValue("@error", ErrorStatus);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0);
    }

    private async Task<PendingSyncRow?> ClaimNextPendingAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
;WITH NextPending AS
(
    SELECT TOP (1) IdSync
    FROM dbo.SYNC_PRECIOS_SERVER WITH (ROWLOCK, READPAST, UPDLOCK)
    WHERE Estado = @pending
    ORDER BY FechaHora, IdSync
)
UPDATE sync
SET Estado = @processing,
    FechaProcesado = NULL,
    Error = NULL
OUTPUT
    INSERTED.IdSync,
    INSERTED.FechaHora,
    INSERTED.FechaUltCambio,
    INSERTED.IdArticulo,
    INSERTED.IdLista,
    INSERTED.Usuario
FROM dbo.SYNC_PRECIOS_SERVER sync
INNER JOIN NextPending pending ON pending.IdSync = sync.IdSync;
""";

        await using var transaction = (SqlTransaction)await localConnection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var cmd = new SqlCommand(sql, localConnection, transaction)
            {
                CommandTimeout = _settings.CommandTimeoutSeconds
            };
            cmd.Parameters.AddWithValue("@pending", PendingStatus);
            cmd.Parameters.AddWithValue("@processing", ProcessingStatus);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await reader.CloseAsync();
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            var row = new PendingSyncRow(
                reader.GetInt32(0),
                reader.GetDateTime(1),
                reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5));

            await reader.CloseAsync();
            await transaction.CommitAsync(cancellationToken);
            return row;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<DataTable> LoadSingleLocalRowAsync(
        SqlConnection localConnection,
        SyncTableSpec spec,
        PendingSyncRow pending,
        CancellationToken cancellationToken)
    {
        var data = new DataTable();
        var whereClause = pending.IdLista is null
            ? "IDARTICULO = @idArticulo"
            : "IdArticulo = @idArticulo AND IdLista = @idLista";

        await using var cmd = new SqlCommand(SyncSqlBuilder.BuildSelectSql(spec, whereClause), localConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@idArticulo", pending.IdArticulo);
        if (pending.IdLista is not null)
            cmd.Parameters.AddWithValue("@idLista", pending.IdLista);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        data.Load(reader);
        return data;
    }

    private async Task UpsertIntoCentralAsync(SyncTableSpec spec, DataTable sourceRow, CancellationToken cancellationToken)
    {
        await using var centralConnection = new SqlConnection(_settings.CentralConnectionString);
        await centralConnection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await centralConnection.BeginTransactionAsync(cancellationToken);
        var tempTableName = $"#Pending_{spec.TempSuffix}_{Guid.NewGuid():N}";

        try
        {
            await ExecuteNonQueryAsync(centralConnection, transaction, SyncSqlBuilder.BuildCreateTempTableSql(tempTableName, spec), cancellationToken);
            await BulkCopyAsync(centralConnection, transaction, tempTableName, sourceRow, cancellationToken);
            await ExecuteScalarIntAsync(centralConnection, transaction, SyncSqlBuilder.BuildUpdateSql(tempTableName, spec), cancellationToken);
            await ExecuteScalarIntAsync(centralConnection, transaction, SyncSqlBuilder.BuildInsertSql(tempTableName, spec), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task MarkProcessedAsync(SqlConnection localConnection, int idSync, CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE dbo.SYNC_PRECIOS_SERVER
SET Estado = @processed,
    FechaProcesado = GETDATE(),
    Error = NULL
WHERE IdSync = @idSync;
""";
        await using var cmd = new SqlCommand(sql, localConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@processed", ProcessedStatus);
        cmd.Parameters.AddWithValue("@idSync", idSync);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task RequeuePendingAsync(SqlConnection localConnection, int idSync, string message, CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE dbo.SYNC_PRECIOS_SERVER
SET Estado = @pending,
    FechaProcesado = NULL,
    Error = @message
WHERE IdSync = @idSync;
""";
        await using var cmd = new SqlCommand(sql, localConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@pending", PendingStatus);
        cmd.Parameters.AddWithValue("@idSync", idSync);
        cmd.Parameters.AddNullableValue("@message", Truncate(message));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkErrorAsync(SqlConnection localConnection, int idSync, string message, CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE dbo.SYNC_PRECIOS_SERVER
SET Estado = @error,
    FechaProcesado = GETDATE(),
    Error = @message
WHERE IdSync = @idSync;
""";
        await using var cmd = new SqlCommand(sql, localConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@error", ErrorStatus);
        cmd.Parameters.AddWithValue("@idSync", idSync);
        cmd.Parameters.AddNullableValue("@message", Truncate(message));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> ExecuteScalarIntAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0);
    }

    private async Task BulkCopyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tempTableName,
        DataTable data,
        CancellationToken cancellationToken)
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = tempTableName,
            BulkCopyTimeout = _settings.CommandTimeoutSeconds
        };

        foreach (DataColumn column in data.Columns)
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulkCopy.WriteToServerAsync(data, cancellationToken);
    }

    private static string Truncate(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        return message.Length <= 4000 ? message : message[..4000];
    }

    private sealed record PendingSyncRow(
        int IdSync,
        DateTime FechaHora,
        DateTime? FechaUltCambio,
        string IdArticulo,
        string? IdLista,
        string? Usuario);
}

public sealed class PendingSyncSummary
{
    public bool TableExists { get; init; }
    public bool PendingFound { get; set; }
    public int ProcessedCount { get; set; }
    public int ErrorCount { get; set; }

    public static PendingSyncSummary TableMissing()
        => new() { TableExists = false };
}
