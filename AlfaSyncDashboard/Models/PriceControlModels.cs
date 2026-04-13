namespace AlfaSyncDashboard.Models;

public enum PriceControlMode
{
    Cost,
    PriceList
}

public sealed class PriceControlRequest
{
    public PriceControlMode Mode { get; set; } = PriceControlMode.Cost;
    public int Limit { get; set; } = 100;
    public bool LoadAll { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public string PriceListId { get; set; } = string.Empty;
    public string TipoLista { get; set; } = string.Empty;
    public int PriceColumn { get; set; } = 1;
}

public sealed class PriceControlRow
{
    public string ListId { get; set; } = string.Empty;
    public string TipoLista { get; set; } = string.Empty;
    public string ArticleId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? CentralValue { get; set; }
    public Dictionary<string, decimal?> LocalValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string ComparisonKey
        => $"{ListId}|{TipoLista}|{ArticleId}";
}

public sealed class PriceControlResult
{
    public List<PriceControlRow> Rows { get; set; } = new();
    public Dictionary<string, string> LocalHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> LocalErrors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> LocalMatches { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string CentralColumnTitle { get; set; } = "Central";
}

public sealed class PriceListOption
{
    public string IdLista { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoLista { get; set; } = string.Empty;

    public override string ToString()
        => string.IsNullOrWhiteSpace(TipoLista)
            ? $"{IdLista} - {Nombre}"
            : $"{IdLista} - {Nombre} ({TipoLista})";
}
