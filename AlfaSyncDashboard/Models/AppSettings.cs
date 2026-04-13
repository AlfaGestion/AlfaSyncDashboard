namespace AlfaSyncDashboard.Models;

public sealed class AppSettings
{
    public string DefaultScriptsPath { get; set; } = @"C:\TAREASALFA";
    public string CentralConnectionString { get; set; } = string.Empty;
    public int MaxParallelLocalTasks { get; set; } = 1;
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public int CommandTimeoutSeconds { get; set; } = 0;
    public List<LocalScriptMapping> LocalScriptMappings { get; set; } = new();
    public Dictionary<string, ScriptSet> ScriptSets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public WindowsServiceSettings WindowsService { get; set; } = new();
    public SyncStatisticsSettings SyncStatistics { get; set; } = new();
}

public sealed class LocalScriptMapping
{
    public string MatchType { get; set; } = "Default";
    public string MatchValue { get; set; } = "*";
    public string ScriptSet { get; set; } = "DEFAULT";
}

public sealed class ScriptSet
{
    public string FamiliesScript { get; set; } = string.Empty;
    public string ArticlesScript { get; set; } = string.Empty;
    public string PriceCabScript { get; set; } = string.Empty;
    public string PricesScript { get; set; } = string.Empty;
}

public sealed class WindowsServiceSettings
{
    public bool Enabled { get; set; }
    public string ServiceName { get; set; } = "Alfa Sincronizacion PDV Sync";
    public int IntervalMinutes { get; set; } = 60;
    public string LocalCodes { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = nameof(SyncExecutionMode.PricesAndCosts);
}

public sealed class SyncStatisticsSettings
{
    public string ApiUrl { get; set; } = "https://alfanet.ddns.net/AGW/V1/control/estadisticas";
    public string IdCliente { get; set; } = string.Empty;
}
