using AlfaSyncDashboard.Models;
using System.Diagnostics;

namespace AlfaSyncDashboard.Services;

public sealed class SyncRunnerService
{
    private readonly LocalPendingSyncService _localPendingSyncService;
    private readonly ScriptExecutionService _scriptExecutionService;
    private readonly SyncLogService _logService;
    private readonly ExecutionLockService _lockService;

    public SyncRunnerService(
        LocalPendingSyncService localPendingSyncService,
        ScriptExecutionService scriptExecutionService,
        SyncLogService logService,
        ExecutionLockService lockService)
    {
        _localPendingSyncService = localPendingSyncService;
        _scriptExecutionService = scriptExecutionService;
        _logService = logService;
        _lockService = lockService;
    }

    public async Task RunAsync(
        IReadOnlyList<TpvInfo> selected,
        SyncExecutionMode mode,
        Action<ExecutionProgress>? reportProgress,
        Action<string> appendLog,
        Action<TpvInfo, string>? updateLocalState,
        CancellationToken cancellationToken)
    {
        if (selected.Count == 0)
            throw new InvalidOperationException("No hay locales seleccionados para sincronizar.");

        if (!_lockService.TryAcquire(out var lockHandle, out var lockMessage))
            throw new InvalidOperationException(lockMessage);

        using (lockHandle)
        {
            await _logService.EnsureTableAsync(cancellationToken);

            for (int i = 0; i < selected.Count; i++)
            {
                var tpv = selected[i];
                var localStopwatch = Stopwatch.StartNew();
                try
                {
                    updateLocalState?.Invoke(tpv, "Sincronizando...");
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Inicio de sincronización", "RUNNING", cancellationToken);
                    await _localPendingSyncService.ProcessBeforePushAsync(tpv, appendLog, cancellationToken);

                    var stageResults = await _scriptExecutionService.ExecuteForLocalAsync(
                        tpv,
                        mode,
                        progress => reportProgress?.Invoke(progress),
                        appendLog,
                        cancellationToken);

                    foreach (var stageResult in stageResults)
                    {
                        await _logService.WriteAsync(
                            tpv.Descripcion,
                            stageResult.StageDisplayName,
                            BuildStageLogMessage(stageResult),
                            "OK",
                            cancellationToken);
                    }

                    tpv.EstadoActual = "OK";
                    tpv.UltimaSincronizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    localStopwatch.Stop();
                    var totalMessage = $"Sincronización OK | duración total={FormatDuration(localStopwatch.Elapsed)} | etapas={stageResults.Count}";
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), totalMessage, "OK", cancellationToken);
                    appendLog($"[{tpv.Descripcion}] sincronización finalizada correctamente. Duración total: {FormatDuration(localStopwatch.Elapsed)}.");
                }
                catch (OperationCanceledException)
                {
                    localStopwatch.Stop();
                    tpv.EstadoActual = "Cancelado";
                    appendLog($"[{tpv.Descripcion}] proceso cancelado. Duración acumulada: {FormatDuration(localStopwatch.Elapsed)}.");
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), $"Cancelado | duración acumulada={FormatDuration(localStopwatch.Elapsed)}", "CANCEL", CancellationToken.None);
                    throw;
                }
                catch (Exception ex)
                {
                    localStopwatch.Stop();
                    tpv.EstadoActual = "ERROR";
                    appendLog($"[{tpv.Descripcion}] error: {ex.Message}");
                    await _logService.WriteAsync(
                        tpv.Descripcion,
                        mode.ToString(),
                        $"Duración hasta error={FormatDuration(localStopwatch.Elapsed)} | {ex}",
                        "ERROR",
                        CancellationToken.None);
                }
                finally
                {
                    updateLocalState?.Invoke(tpv, tpv.EstadoActual);
                }
            }
        }
    }

    private static string BuildStageLogMessage(StageExecutionResult result)
        => $"Central={result.SourceRowCount} | Insertados={result.InsertedCount} | Actualizados={result.UpdatedCount} | Duración={FormatDuration(result.Duration)}";

    private static string FormatDuration(TimeSpan duration)
        => duration.ToString(@"hh\:mm\:ss");
}
