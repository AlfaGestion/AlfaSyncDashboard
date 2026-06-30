namespace AlfaSyncDashboard.Models;

public sealed class StageExecutionResult
{
    public string StageDisplayName { get; set; } = string.Empty;
    public int SourceRowCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public TimeSpan Duration { get; set; }
}
