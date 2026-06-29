using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Services;

public sealed class CentralDataService
{
    private readonly AppSettings _settings;

    public CentralDataService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<List<TpvInfo>> LoadTpvsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT CODIGO, SUCURSAL, DESCRIPCION, SERVER, DBNAME, USUARIO, PASSWORD
FROM dbo.V_TA_TPV WHERE BAJA = 0
ORDER BY DESCRIPCION;";

        var selectedCodes = new HashSet<string>(_settings.SelectedLocalCodes ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var result = new List<TpvInfo>();
        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };

        await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await dr.ReadAsync(cancellationToken))
        {
            var tpv = new TpvInfo
            {
                Selected = selectedCodes.Contains(dr["CODIGO"]?.ToString()?.Trim() ?? string.Empty),
                Codigo = dr["CODIGO"]?.ToString()?.Trim() ?? string.Empty,
                Sucursal = dr["SUCURSAL"]?.ToString()?.Trim() ?? string.Empty,
                Descripcion = dr["DESCRIPCION"]?.ToString()?.Trim() ?? string.Empty,
                Server = dr["SERVER"]?.ToString()?.Trim() ?? string.Empty,
                DbName = dr["DBNAME"]?.ToString()?.Trim() ?? string.Empty,
                Usuario = dr["USUARIO"]?.ToString()?.Trim() ?? string.Empty,
                Password = dr["PASSWORD"]?.ToString() ?? string.Empty,
            };
            tpv.ScriptSet = ResolveScriptSet(tpv);
            result.Add(tpv);
        }

        return result;
    }

    public string ResolveScriptSet(TpvInfo tpv)
    {
        foreach (var mapping in _settings.LocalScriptMappings)
        {
            if (mapping.MatchType.Equals("Codigo", StringComparison.OrdinalIgnoreCase)
                && string.Equals(tpv.Codigo, mapping.MatchValue, StringComparison.OrdinalIgnoreCase))
                return mapping.ScriptSet;

            if (mapping.MatchType.Equals("DescriptionContains", StringComparison.OrdinalIgnoreCase)
                && tpv.Descripcion.Contains(mapping.MatchValue, StringComparison.OrdinalIgnoreCase))
                return mapping.ScriptSet;

            if (mapping.MatchType.Equals("Default", StringComparison.OrdinalIgnoreCase))
                return mapping.ScriptSet;
        }

        return "DEFAULT";
    }

    public async Task<bool> TestCentralConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using var cn = new SqlConnection(_settings.CentralConnectionString);
        await cn.OpenAsync(cancellationToken);
        return cn.State == System.Data.ConnectionState.Open;
    }
}
