using AlfaSyncDashboard.Models;

namespace AlfaSyncDashboard.Services;

public sealed class SyncRunnerService
{
    private readonly ScriptExecutionService _scriptExecutionService;
    private readonly SyncLogService _logService;
    private readonly ExecutionLockService _lockService;

    public SyncRunnerService(
        ScriptExecutionService scriptExecutionService,
        SyncLogService logService,
        ExecutionLockService lockService)
    {
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
                try
                {
                    updateLocalState?.Invoke(tpv, "Sincronizando...");
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Inicio de sincronización", "RUNNING", cancellationToken);

                    await _scriptExecutionService.ExecuteForLocalAsync(
                        tpv,
                        mode,
                        progress => reportProgress?.Invoke(progress),
                        appendLog,
                        cancellationToken);

                    tpv.EstadoActual = "OK";
                    tpv.UltimaSincronizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Sincronización OK", "OK", cancellationToken);
                    appendLog($"[{tpv.Descripcion}] sincronización finalizada correctamente.");
                }
                catch (OperationCanceledException)
                {
                    tpv.EstadoActual = "Cancelado";
                    appendLog($"[{tpv.Descripcion}] proceso cancelado.");
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Cancelado", "CANCEL", CancellationToken.None);
                    throw;
                }
                catch (Exception ex)
                {
                    tpv.EstadoActual = "ERROR";
                    appendLog($"[{tpv.Descripcion}] error: {ex.Message}");
                    await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), ex.ToString(), "ERROR", CancellationToken.None);
                }
                finally
                {
                    updateLocalState?.Invoke(tpv, tpv.EstadoActual);
                }
            }
        }
    }
}
