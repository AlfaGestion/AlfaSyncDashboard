namespace AlfaSyncDashboard.Models;

public sealed class AppSettings
{
    public string DefaultScriptsPath { get; set; } = @"C:\TAREASALFA";
    public string CentralConnectionString { get; set; } = string.Empty;
    public int MaxParallelLocalTasks { get; set; } = 1;
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public int CommandTimeoutSeconds { get; set; } = 0;
    public bool DisableDestinationTriggersDuringSync { get; set; } = false;
    public List<string> SelectedLocalCodes { get; set; } = new();
    public List<LocalScriptMapping> LocalScriptMappings { get; set; } = new();
    public Dictionary<string, ScriptSet> ScriptSets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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
