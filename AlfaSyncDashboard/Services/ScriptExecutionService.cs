using System.Data;
using System.Diagnostics;
using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

public sealed class ScriptExecutionService
{
    private const string SalesTipoLista = "V";
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

            var spec = ResolveSpec(stage.FileName);
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
                await ExecuteNonQueryAsync(localConnection, transaction, BuildDisableTriggersSql(spec), cancellationToken);
            }

            await CreateTempTableAsync(localConnection, transaction, tempTableName, spec, cancellationToken);
            await BulkCopyToTempTableAsync(localConnection, transaction, tempTableName, sourceTable, cancellationToken);
            var updated = await ExecuteScalarIntAsync(localConnection, transaction, BuildUpdateSql(tempTableName, spec), cancellationToken);
            var inserted = await ExecuteScalarIntAsync(localConnection, transaction, BuildInsertSql(tempTableName, spec), cancellationToken);
            result.UpdatedCount = updated;
            result.InsertedCount = inserted;

            if (_settings.DisableDestinationTriggersDuringSync)
            {
                await ExecuteNonQueryAsync(localConnection, transaction, BuildEnableTriggersSql(spec), cancellationToken);
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
        await using var cmd = new SqlCommand(BuildSelectSql(spec), centralConnection)
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
        var sql = $"SELECT TOP 0 {JoinColumns(spec.Columns)} INTO {tempTableName} FROM {spec.TargetObject};";
        await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken);
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

    private static string BuildSelectSql(SyncTableSpec spec)
        => $"SELECT {BuildSelectColumns(spec)} FROM {spec.SourceObject};";

    private static string BuildUpdateSql(string tempTableName, SyncTableSpec spec)
    {
        var updateColumns = spec.Columns.Except(spec.KeyColumns, StringComparer.OrdinalIgnoreCase).ToArray();
        var setClause = string.Join(", ", updateColumns.Select(col => $"target.[{col}] = source.[{col}]"));
        var changeDetection = BuildChangeDetectionSql(updateColumns);
        return $@"
UPDATE target
SET {setClause}
FROM {spec.TargetObject} target
INNER JOIN {tempTableName} source ON {BuildJoinCondition(spec.KeyColumns)}
WHERE {changeDetection}
SELECT @@ROWCOUNT;";
    }

    private static string BuildInsertSql(string tempTableName, SyncTableSpec spec)
    {
        var columns = JoinColumns(spec.Columns);
        var sourceColumns = string.Join(", ", spec.Columns.Select(col => $"source.[{col}]"));
        return $@"
INSERT INTO {spec.TargetObject} ({columns})
SELECT {sourceColumns}
FROM {tempTableName} source
WHERE NOT EXISTS (
    SELECT 1
    FROM {spec.TargetObject} target
    WHERE {BuildJoinCondition(spec.KeyColumns)}
);
SELECT @@ROWCOUNT;";
    }

    private static string BuildDisableTriggersSql(SyncTableSpec spec)
        => $"DISABLE TRIGGER ALL ON {spec.TargetObject};";

    private static string BuildEnableTriggersSql(SyncTableSpec spec)
        => $"ENABLE TRIGGER ALL ON {spec.TargetObject};";

    private static string BuildJoinCondition(IReadOnlyList<string> keyColumns)
        => string.Join(" AND ", keyColumns.Select(col => $"target.[{col}] = source.[{col}]"));

    private static string BuildChangeDetectionSql(IReadOnlyList<string> columns)
    {
        var sourceColumns = string.Join(", ", columns.Select(col => $"source.[{col}]"));
        var targetColumns = string.Join(", ", columns.Select(col => $"target.[{col}]"));
        return $"EXISTS (SELECT {sourceColumns} EXCEPT SELECT {targetColumns})";
    }

    private static string JoinColumns(IEnumerable<string> columns)
        => string.Join(", ", columns.Select(col => $"[{col}]"));

    private static string BuildSelectColumns(SyncTableSpec spec)
        => string.Join(", ", spec.Columns.Select(col => BuildSelectColumn(spec, col)));

    private static string BuildSelectColumn(SyncTableSpec spec, string column)
    {
        if (spec.FixedValues.TryGetValue(column, out var value))
            return $"'{value.Replace("'", "''")}' AS [{column}]";

        return $"[{column}]";
    }

    private static SyncTableSpec ResolveSpec(string fileName)
    {
        var name = Path.GetFileName(fileName).ToUpperInvariant();
        return name switch
        {
            "ACTUALIZA_FAMILIAS.SQL" => new SyncTableSpec(
                "Familias",
                "FAMILIAS",
                "[dbo].[V_TA_FAMILIAS]",
                new[] { "IdFamilia" },
                new[] { "IdFamilia", "Descripcion", "Transmision", "MKBase", "MkReal", "IdPolitica" }),
            "ACTUALIZA_CATEGORIAS_ARTICULO.SQL" => new SyncTableSpec(
                "Categorías artículo",
                "CATEGORIASART",
                "[dbo].[V_TA_CategoriaArticulo]",
                new[] { "IdCategoria" },
                new[] { "IdCategoria", "Descripcion" }),
            "ACTUALIZA_RUBROS.SQL" => new SyncTableSpec(
                "Rubros",
                "RUBROS",
                "[dbo].[V_TA_Rubros]",
                new[] { "IdRubro" },
                new[] { "IdRubro", "Descripcion", "Transmision", "COLOR", "Pesable" }),
            "ACTUALIZA_UNIDADES.SQL" => new SyncTableSpec(
                "Unidades",
                "UNIDADES",
                "[dbo].[V_TA_Unidad]",
                new[] { "IdUnidad" },
                new[] { "IdUnidad", "Descripcion", "Transmision" }),
            "ACTUALIZA_TIPOS_ARTICULO.SQL" => new SyncTableSpec(
                "Tipos artículo",
                "TIPOSART",
                "[dbo].[V_TA_TipoArticulo]",
                new[] { "IdTipo" },
                new[] { "IdTipo", "Descripcion", "Transmision" }),
            "ACTUALIZA_ARTICULOS.SQL" => new SyncTableSpec(
                "Artículos",
                "ARTICULOS",
                "[dbo].[V_MA_ARTICULOS]",
                new[] { "IDARTICULO" },
                new[]
                {
                    "IDARTICULO", "CODIGOBARRA", "DESCRIPCION", "IDUNIDAD", "IDRUBRO", "IDTIPO", "USASERIE", "USALOTE",
                    "EXENTO", "NOTAS", "COSTO", "IMPUESTOS", "PRECIO1", "PRECIO2", "PRECIO3", "PRECIO4", "PRECIO5",
                    "PoliticaPrecios", "TasaIVA", "Moneda", "RutaImagen", "IdPercepcion", "Usuario", "Observaciones",
                    "ActualizaCosto", "IdFamilia", "UD_CPRA", "UD_STOCK", "UD_TTE", "Presentacion", "DescripAbrev",
                    "CostoInsumos", "DesdeTrigger", "CodigoBarraDun", "Perecedero", "Pesable", "PRECIO6", "PRECIO7",
                    "UTILIDAD", "CODIGOBARRA1", "CODIGOBARRA2", "CODIGOBARRA3", "CODIGOBARRA4", "UMCB1", "UMCB2",
                    "UMCB3", "UMCB4", "PRECIO8", "KILOS", "M3", "Dto1", "Dto2", "Dto3", "Dto4", "Dto5", "Rec1", "Rec2",
                    "Rec3", "IdTarifaFlete", "PorcSeguro", "Dto6", "Dto7", "Dto8", "Dto9", "FhUltimoCosto", "FhDtoDesde",
                    "FhDtoHasta", "UsaTalle", "CodigoArtProveedor", "TalleDefault", "ColorDefault", "SexoDefault",
                    "InsumosPorPorcentaje", "EnOferta", "Espesor", "Ancho", "Largo", "PideMedidas", "EnComodato",
                    "Transmision", "PideEquivalencia", "ITC", "FHALTA", "InsertaObserv", "NO_CONTROLA_STOCK", "URL1",
                    "Ubicacion_Habitual", "Procedencia", "PideDescripcionAdicional"
                }),
            "ACTUALIZA_V_MA_PRECIOS.SQL" => new SyncTableSpec(
                "Precios",
                "PRECIOS",
                "[dbo].[V_MA_PRECIOS]",
                new[] { "IdLista", "IdArticulo" },
                new[]
                {
                    "IdLista", "Nombre", "IdArticulo", "ConIVA", "Precio1", "Precio2", "Precio3", "Precio4", "Precio5",
                    "IdMoneda", "TipoLista", "FCOSTO", "FCLASE1", "FCLASE2", "FCLASE3", "FCLASE4", "FCLASE5", "COSTO",
                    "ActualizaBase", "POLITICAPRECIOS", "CUENTAPROVEEDOR", "IDARTICULOPROVEEDOR", "DESCRIPCIONARTICULO",
                    "RUBRO", "TIPO", "IdUnidad", "DesdeTrigger", "FhOfertaDesde", "FhOfertaHasta", "CantidadDesde", "GRUPO",
                    "Precio0", "PRECIO6", "PRECIO7", "UTILIDAD", "FCLASE6", "FCLASE7", "PRECIO8", "FCLASE8",
                    "ModificaPrecios", "Dto1", "Dto2", "Dto3", "Dto4", "Dto5", "Dto6", "Dto7", "Dto8", "Dto9", "Rec1",
                    "Rec2", "Rec3", "IdTarifaFlete", "PorcSeguro", "FhDtoDesde", "FhDtoHasta", "IDCOMPULSA", "IDINSERT",
                    "ESTADO", "MKTeorico", "CambioCodBarra", "USUARIO", "FHALTA", "PRECIO9", "CantidadOf2", "PRECIO10",
                    "CantidadOf3"
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["TipoLista"] = SalesTipoLista
                }),
            "ACTUALIZA_V_MA_PRECIOSCAB.SQL" => new SyncTableSpec(
                "PreciosCab",
                "PRECIOSCAB",
                "[dbo].[V_MA_PRECIOSCAB]",
                new[] { "IdLista", "TipoLista" },
                new[] { "IdLista", "Nombre", "Grupo", "VigenciaDesde", "VigenciaHasta", "TipoLista" },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["TipoLista"] = SalesTipoLista
                }),
            _ => throw new InvalidOperationException($"No existe una definición de sincronización directa para '{fileName}'.")
        };
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

    private sealed record SyncTableSpec(
        string DisplayName,
        string TempSuffix,
        string TargetObject,
        IReadOnlyList<string> KeyColumns,
        IReadOnlyList<string> Columns,
        IReadOnlyDictionary<string, string>? fixedValues = null)
    {
        public string SourceObject => TargetObject;
        public IReadOnlyDictionary<string, string> FixedValues { get; } = fixedValues ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
