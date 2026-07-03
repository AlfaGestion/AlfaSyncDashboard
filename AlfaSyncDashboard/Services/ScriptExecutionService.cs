using System.Data;
using System.Diagnostics;
using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

public sealed class ScriptExecutionService
{
    private readonly AppSettings _settings;

    public ScriptExecutionService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<StageExecutionResult>> ExecuteForLocalAsync(
        TpvInfo tpv,
        SyncExecutionMode mode,
        Action<ExecutionProgress> reportProgress,
        Action<string> appendLog,
        CancellationToken cancellationToken)
    {
        if (!_settings.ScriptSets.TryGetValue(tpv.ScriptSet, out var scriptSet))
            throw new InvalidOperationException($"No existe el ScriptSet '{tpv.ScriptSet}' para el local {tpv.Descripcion}.");

        var stages = BuildStages(scriptSet, mode);
        var stopwatch = Stopwatch.StartNew();
        var results = new List<StageExecutionResult>(stages.Count);

        for (int i = 0; i < stages.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stage = stages[i];
            if (string.IsNullOrWhiteSpace(stage.FileName))
                throw new InvalidOperationException($"No está configurado el archivo SQL para la etapa '{stage.DisplayName}' en el ScriptSet '{tpv.ScriptSet}'.");

            var overallPercent = (int)Math.Round((i / (double)stages.Count) * 100d);
            reportProgress(new ExecutionProgress
            {
                LocalDescripcion = tpv.Descripcion,
                Etapa = stage.DisplayName,
                StageIndex = i + 1,
                TotalStages = stages.Count,
                OverallPercent = overallPercent,
                Elapsed = stopwatch.Elapsed,
                EstimatedRemaining = EstimateRemaining(stopwatch.Elapsed, i, stages.Count),
                Message = $"Iniciando {stage.DisplayName}"
            });

            var spec = SyncMetadata.ResolveSpec(stage.FileName);
            appendLog($"[{tpv.Descripcion}] Sincronizando {stage.DisplayName} por conexión directa al local.");
            var stageResult = await SyncDirectAsync(tpv, spec, appendLog, cancellationToken);
            stageResult.StageDisplayName = stage.DisplayName;
            results.Add(stageResult);

            overallPercent = (int)Math.Round(((i + 1) / (double)stages.Count) * 100d);
            reportProgress(new ExecutionProgress
            {
                LocalDescripcion = tpv.Descripcion,
                Etapa = stage.DisplayName,
                StageIndex = i + 1,
                TotalStages = stages.Count,
                OverallPercent = overallPercent,
                Elapsed = stopwatch.Elapsed,
                EstimatedRemaining = EstimateRemaining(stopwatch.Elapsed, i + 1, stages.Count),
                Message = $"Finalizó {stage.DisplayName}"
            });
        }

        return results;
    }

    private async Task<StageExecutionResult> SyncDirectAsync(TpvInfo tpv, SyncTableSpec spec, Action<string> appendLog, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var sourceTable = await LoadSourceDataAsync(spec, cancellationToken);
        var result = new StageExecutionResult
        {
            StageDisplayName = spec.DisplayName,
            SourceRowCount = sourceTable.Rows.Count
        };

        appendLog($"[{tpv.Descripcion}] {spec.DisplayName}: {sourceTable.Rows.Count} filas leídas desde central.");

        if (sourceTable.Rows.Count == 0)
        {
            result.Duration = stopwatch.Elapsed;
            appendLog($"[{tpv.Descripcion}] {spec.DisplayName}: central={result.SourceRowCount} | actualizados=0 | insertados=0 | duración={FormatDuration(result.Duration)}");
            return result;
        }

        await using var localConnection = new SqlConnection(tpv.BuildLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        var transaction = (SqlTransaction)await localConnection.BeginTransactionAsync(cancellationToken);

        var tempTableName = $"#Sync_{spec.TempSuffix}_{Guid.NewGuid():N}";

        try
        {
            if (_settings.DisableDestinationTriggersDuringSync)
            {
                appendLog($"[{tpv.Descripcion}] {spec.DisplayName}: desactivando triggers en destino.");
                await ExecuteNonQueryAsync(localConnection, transaction, SyncSqlBuilder.BuildDisableTriggersSql(spec), cancellationToken);
            }

            await CreateTempTableAsync(localConnection, transaction, tempTableName, spec, cancellationToken);
            await BulkCopyToTempTableAsync(localConnection, transaction, tempTableName, sourceTable, cancellationToken);
            var updated = await ExecuteScalarIntAsync(localConnection, transaction, SyncSqlBuilder.BuildUpdateSql(tempTableName, spec), cancellationToken);
            var inserted = await ExecuteScalarIntAsync(localConnection, transaction, SyncSqlBuilder.BuildInsertSql(tempTableName, spec), cancellationToken);
            result.UpdatedCount = updated;
            result.InsertedCount = inserted;

            if (_settings.DisableDestinationTriggersDuringSync)
            {
                await ExecuteNonQueryAsync(localConnection, transaction, SyncSqlBuilder.BuildEnableTriggersSql(spec), cancellationToken);
                appendLog($"[{tpv.Descripcion}] {spec.DisplayName}: triggers reactivados en destino.");
            }

            await transaction.CommitAsync(cancellationToken);
            result.Duration = stopwatch.Elapsed;
            appendLog($"[{tpv.Descripcion}] {spec.DisplayName}: central={result.SourceRowCount} | actualizados={result.UpdatedCount} | insertados={result.InsertedCount} | duración={FormatDuration(result.Duration)}");
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<DataTable> LoadSourceDataAsync(SyncTableSpec spec, CancellationToken cancellationToken)
    {
        var data = new DataTable();
        await using var centralConnection = new SqlConnection(_settings.CentralConnectionString);
        await centralConnection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(SyncSqlBuilder.BuildSelectSql(spec), centralConnection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        data.Load(reader);
        return data;
    }

    private async Task CreateTempTableAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tempTableName,
        SyncTableSpec spec,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(connection, transaction, SyncSqlBuilder.BuildCreateTempTableSql(tempTableName, spec), cancellationToken);
    }

    private async Task BulkCopyToTempTableAsync(
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

    private async Task<int> ExecuteScalarIntAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0);
    }

    private async Task ExecuteNonQueryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static TimeSpan EstimateRemaining(TimeSpan elapsed, int completedStages, int totalStages)
    {
        if (completedStages <= 0 || totalStages <= 0)
            return TimeSpan.Zero;

        var avgTicks = elapsed.Ticks / completedStages;
        var pending = totalStages - completedStages;
        return new TimeSpan(avgTicks * pending);
    }

    private static string FormatDuration(TimeSpan duration)
        => duration.ToString(@"hh\:mm\:ss");

    private static List<(string DisplayName, string FileName)> BuildStages(ScriptSet set, SyncExecutionMode mode)
    {
        var list = new List<(string DisplayName, string FileName)>();
        if (mode == SyncExecutionMode.Full)
        {
            list.Add(("Categorías artículo", set.CategoriesScript));
            list.Add(("Rubros", set.RubrosScript));
            list.Add(("Unidades", set.UnitsScript));
            list.Add(("Tipos artículo", set.ArticleTypesScript));
            list.Add(("Familias", set.FamiliesScript));
        }

        list.Add(("Artículos", set.ArticlesScript));
        list.Add(("PreciosCab", set.PriceCabScript));
        list.Add(("Precios", set.PricesScript));
        return list;
    }
}
