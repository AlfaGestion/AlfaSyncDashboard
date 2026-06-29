using System.Data;
using System.Text;
using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

public sealed class PriceControlService
{
    private const string SalesTipoLista = "V";
    private readonly AppSettings _settings;

    public PriceControlService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<PriceControlResult> LoadAsync(
        IReadOnlyList<TpvInfo> locals,
        PriceControlRequest request,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadCentralRowsAsync(request, cancellationToken);
        var result = new PriceControlResult
        {
            Rows = rows,
            CentralColumnTitle = BuildCentralTitle(request)
        };

        foreach (var local in locals)
        {
            var key = BuildLocalKey(local);
            result.LocalHeaders[key] = local.Descripcion;

            try
            {
                var values = await LoadLocalValuesAsync(local, rows, request, cancellationToken);
                result.LocalMatches[key] = values.Count;
                foreach (var row in rows)
                    row.LocalValues[key] = values.TryGetValue(row.ArticleId, out var value) ? value : null;
            }
            catch (Exception ex)
            {
                result.LocalErrors[key] = ex.Message;
                result.LocalMatches[key] = 0;
                foreach (var row in rows)
                    row.LocalValues[key] = null;
            }
        }

        return result;
    }

    public async Task<List<PriceListOption>> LoadAvailableListsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT DISTINCT IdLista, Nombre, TipoLista
FROM dbo.V_MA_PRECIOSCAB
WHERE LTRIM(RTRIM(TipoLista)) = 'V'
ORDER BY Nombre, IdLista, TipoLista;";

        var result = new List<PriceListOption>();
        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };

        await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await dr.ReadAsync(cancellationToken))
        {
            result.Add(new PriceListOption
            {
                IdLista = dr["IdLista"]?.ToString()?.Trim() ?? string.Empty,
                Nombre = dr["Nombre"]?.ToString()?.Trim() ?? string.Empty,
                TipoLista = dr["TipoLista"]?.ToString()?.Trim() ?? string.Empty
            });
        }

        return result;
    }

    private async Task<List<PriceControlRow>> LoadCentralRowsAsync(PriceControlRequest request, CancellationToken cancellationToken)
    {
        var sql = BuildCentralSql(request);
        var rows = new List<PriceControlRow>();

        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };

        AddRequestParameters(cmd, request);

        await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await dr.ReadAsync(cancellationToken))
        {
            rows.Add(new PriceControlRow
            {
                ArticleId = dr["IDARTICULO"]?.ToString()?.Trim() ?? string.Empty,
                Description = dr["DESCRIPCION"]?.ToString()?.Trim() ?? string.Empty,
                CentralValue = dr.IsDBNull(dr.GetOrdinal("VALOR")) ? null : dr.GetDecimal(dr.GetOrdinal("VALOR"))
            });
        }

        return rows;
    }

    private async Task<Dictionary<string, decimal?>> LoadLocalValuesAsync(
        TpvInfo local,
        IReadOnlyList<PriceControlRow> centralRows,
        PriceControlRequest request,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        if (centralRows.Count == 0)
            return result;

        var articleIds = centralRows
            .Select(x => x.ArticleId.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sql = BuildLocalSql(articleIds, request);
        await using var cn = new SqlConnection(local.BuildLocalConnectionString());
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };

        AddRequestParameters(cmd, request);
        AddArticleParameters(cmd, articleIds);

        await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await dr.ReadAsync(cancellationToken))
        {
            var articleId = dr["IDARTICULO"]?.ToString()?.Trim() ?? string.Empty;
            decimal? value = dr.IsDBNull(dr.GetOrdinal("VALOR")) ? null : dr.GetDecimal(dr.GetOrdinal("VALOR"));
            if (!string.IsNullOrWhiteSpace(articleId))
                result[articleId] = value;
        }

        return result;
    }

    private static void AddRequestParameters(SqlCommand cmd, PriceControlRequest request)
    {
        var tipoLista = NormalizeTipoLista(request);
        cmd.Parameters.AddWithValue("@limit", request.Limit);
        cmd.Parameters.AddWithValue("@search", request.SearchText.Trim());
        cmd.Parameters.AddWithValue("@searchLike", $"%{request.SearchText.Trim()}%");
        cmd.Parameters.AddWithValue("@priceListId", request.PriceListId.Trim());
        cmd.Parameters.AddWithValue("@tipoLista", tipoLista);
    }

    private static void AddArticleParameters(SqlCommand cmd, string[] articleIds)
    {
        for (var i = 0; i < articleIds.Length; i++)
            cmd.Parameters.AddWithValue($"@id{i}", articleIds[i]);
    }

    private static string BuildCentralSql(PriceControlRequest request)
    {
        return request.Mode switch
        {
            PriceControlMode.Cost => @"
WITH Base AS
(
    SELECT
        P.IDARTICULO,
        A.DESCRIPCION,
        CAST(P.COSTO AS decimal(18,4)) AS VALOR,
        ROW_NUMBER() OVER (PARTITION BY P.IDARTICULO ORDER BY P.TipoLista, P.IdLista) AS RN
    FROM dbo.V_MA_PRECIOS P
    INNER JOIN dbo.V_MA_ARTICULOS A ON A.IDARTICULO = P.IDARTICULO
    WHERE (@search = '' OR P.IDARTICULO LIKE @searchLike OR A.DESCRIPCION LIKE @searchLike)
)
SELECT TOP (@limit)
    IDARTICULO,
    DESCRIPCION,
    VALOR
FROM Base
WHERE RN = 1
ORDER BY DESCRIPCION, IDARTICULO;",
            PriceControlMode.PriceList => $@"
WITH Base AS
(
    SELECT
        LTRIM(RTRIM(P.IDARTICULO)) AS IDARTICULO,
        LTRIM(RTRIM(A.DESCRIPCION)) AS DESCRIPCION,
        CAST(P.{GetPriceColumnName(request.PriceColumn)} AS decimal(18,4)) AS VALOR,
        ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(P.IDARTICULO)) ORDER BY LTRIM(RTRIM(P.TipoLista)), LTRIM(RTRIM(P.IdLista))) AS RN
    FROM dbo.V_MA_PRECIOS P
    INNER JOIN dbo.V_MA_ARTICULOS A ON A.IDARTICULO = P.IDARTICULO
    WHERE LTRIM(RTRIM(P.IdLista)) = @priceListId
      AND LTRIM(RTRIM(P.TipoLista)) = @tipoLista
      AND (@search = '' OR LTRIM(RTRIM(P.IDARTICULO)) LIKE @searchLike OR LTRIM(RTRIM(A.DESCRIPCION)) LIKE @searchLike)
)
SELECT TOP (@limit)
    IDARTICULO,
    DESCRIPCION,
    VALOR
FROM Base
WHERE RN = 1
ORDER BY DESCRIPCION, IDARTICULO;",
            _ => throw new InvalidOperationException("Modo de control no soportado.")
        };
    }

    private static string BuildLocalSql(string[] articleIds, PriceControlRequest request)
    {
        var filter = BuildArticleFilter(articleIds, "LTRIM(RTRIM(P.IDARTICULO))");
        return request.Mode switch
        {
            PriceControlMode.Cost => $@"
WITH Base AS
(
    SELECT
        LTRIM(RTRIM(P.IDARTICULO)) AS IDARTICULO,
        CAST(P.COSTO AS decimal(18,4)) AS VALOR,
        ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(P.IDARTICULO)) ORDER BY P.TipoLista, P.IdLista) AS RN
    FROM dbo.V_MA_PRECIOS P
    WHERE {filter}
)
SELECT IDARTICULO, VALOR
FROM Base
WHERE RN = 1;",
            PriceControlMode.PriceList => $@"
WITH Base AS
(
    SELECT
        LTRIM(RTRIM(P.IDARTICULO)) AS IDARTICULO,
        CAST(P.{GetPriceColumnName(request.PriceColumn)} AS decimal(18,4)) AS VALOR,
        ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(P.IDARTICULO)) ORDER BY LTRIM(RTRIM(P.TipoLista)), LTRIM(RTRIM(P.IdLista))) AS RN
    FROM dbo.V_MA_PRECIOS P
    WHERE LTRIM(RTRIM(P.IdLista)) = @priceListId
      AND LTRIM(RTRIM(P.TipoLista)) = @tipoLista
      AND {filter}
)
SELECT IDARTICULO, VALOR
FROM Base
WHERE RN = 1;",
            _ => throw new InvalidOperationException("Modo de control no soportado.")
        };
    }

    private static string BuildArticleFilter(string[] articleIds, string fieldExpression)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < articleIds.Length; i++)
        {
            if (i > 0)
                builder.Append(" OR ");

            builder.Append($"{fieldExpression} = @id{i}");
        }

        return builder.Length == 0 ? "1 = 0" : $"({builder})";
    }

    private static string GetPriceColumnName(int priceColumn)
    {
        if (priceColumn < 1 || priceColumn > 8)
            throw new InvalidOperationException("La clase de precio debe estar entre 1 y 8.");

        return $"PRECIO{priceColumn}";
    }

    private static string BuildLocalKey(TpvInfo local)
        => $"{local.Codigo}|{local.Descripcion}";

    private static string NormalizeTipoLista(PriceControlRequest request)
        => request.Mode == PriceControlMode.PriceList ? SalesTipoLista : request.TipoLista.Trim();

    private static string BuildCentralTitle(PriceControlRequest request)
    {
        return request.Mode switch
        {
            PriceControlMode.Cost => "Costo central",
            PriceControlMode.PriceList => $"Central Lista {request.PriceListId.Trim()} Precio{request.PriceColumn}" +
                                          (string.IsNullOrWhiteSpace(request.TipoLista) ? string.Empty : $" Tipo {request.TipoLista.Trim()}"),
            _ => "Central"
        };
    }
}
