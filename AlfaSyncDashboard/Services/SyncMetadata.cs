using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

internal static class SyncMetadata
{
    private const string SalesTipoLista = "V";

    public static SyncTableSpec ResolveSpec(string fileName)
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
                new[] { "IdTipo", "Descripcion" }),
            "ACTUALIZA_ARTICULOS.SQL" => ArticlesSpec,
            "ACTUALIZA_V_MA_PRECIOS.SQL" => PricesSpec,
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

    public static SyncTableSpec ArticlesSpec { get; } = new(
        "Artículos",
        "ARTICULOS",
        "[dbo].[V_MA_ARTICULOS]",
        new[] { "IDARTICULO" },
        new[]
        {
            "IDARTICULO", "CODIGOBARRA", "DESCRIPCION", "IDUNIDAD", "IDRUBRO", "IDTIPO", "USASERIE", "USALOTE",
            "EXENTO", "NOTAS", "COSTO", "IMPUESTOS", "PRECIO1", "PRECIO2", "PRECIO3", "PRECIO4", "PRECIO5",
            "SUSPENDIDO",
            "PoliticaPrecios", "TasaIVA", "Moneda", "RutaImagen", "IdPercepcion", "Usuario", "Observaciones",
            "ActualizaCosto", "IdFamilia", "UD_CPRA", "UD_STOCK", "UD_TTE", "Presentacion", "DescripAbrev",
            "CostoInsumos", "DesdeTrigger", "CodigoBarraDun", "Perecedero", "Pesable", "PRECIO6", "PRECIO7",
            "UTILIDAD", "CODIGOBARRA1", "CODIGOBARRA2", "CODIGOBARRA3", "CODIGOBARRA4", "UMCB1", "UMCB2",
            "UMCB3", "UMCB4", "PRECIO8", "KILOS", "M3", "SuspendidoV", "SuspendidoC", "Dto1", "Dto2", "Dto3",
            "Dto4", "Dto5", "Rec1", "Rec2",
            "Rec3", "IdTarifaFlete", "PorcSeguro", "Dto6", "Dto7", "Dto8", "Dto9", "FhUltimoCosto", "FhDtoDesde",
            "FhDtoHasta", "UsaTalle", "CodigoArtProveedor", "TalleDefault", "ColorDefault", "SexoDefault",
            "InsumosPorPorcentaje", "EnOferta", "Espesor", "Ancho", "Largo", "PideMedidas", "EnComodato",
            "Transmision", "PideEquivalencia", "ITC", "FHALTA", "InsertaObserv", "NO_CONTROLA_STOCK", "URL1",
            "Ubicacion_Habitual", "Procedencia", "PideDescripcionAdicional"
        });

    public static SyncTableSpec PricesSpec { get; } = new(
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
        });
}

internal sealed record SyncTableSpec(
    string DisplayName,
    string TempSuffix,
    string TargetObject,
    IReadOnlyList<string> KeyColumns,
    IReadOnlyList<string> Columns,
    IReadOnlyDictionary<string, string>? FixedValues = null)
{
    public string SourceObject => TargetObject;
    public IReadOnlyDictionary<string, string> EffectiveFixedValues { get; } = FixedValues ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

internal static class SyncSqlBuilder
{
    public static string BuildSelectSql(SyncTableSpec spec)
        => $"SELECT {BuildSelectColumns(spec)} FROM {spec.SourceObject};";

    public static string BuildSelectSql(SyncTableSpec spec, string whereClause)
        => $"SELECT {BuildSelectColumns(spec)} FROM {spec.SourceObject} WHERE {whereClause};";

    public static string BuildUpdateSql(string tempTableName, SyncTableSpec spec)
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

    public static string BuildInsertSql(string tempTableName, SyncTableSpec spec)
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

    public static string BuildDisableTriggersSql(SyncTableSpec spec)
        => $"DISABLE TRIGGER ALL ON {spec.TargetObject};";

    public static string BuildEnableTriggersSql(SyncTableSpec spec)
        => $"ENABLE TRIGGER ALL ON {spec.TargetObject};";

    public static string BuildCreateTempTableSql(string tempTableName, SyncTableSpec spec)
        => $"SELECT TOP 0 {JoinColumns(spec.Columns)} INTO {tempTableName} FROM {spec.TargetObject};";

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
        if (spec.EffectiveFixedValues.TryGetValue(column, out var value))
            return $"'{value.Replace("'", "''")}' AS [{column}]";

        return $"[{column}]";
    }
}

internal static class SqlParameterExtensions
{
    public static void AddNullableValue(this SqlParameterCollection parameters, string name, object? value)
        => parameters.AddWithValue(name, value ?? DBNull.Value);
}
