using AlfaSyncDashboard.Models;
using Microsoft.Extensions.Hosting;

namespace AlfaSyncDashboard.Services;

public sealed class WindowsSyncWorker : BackgroundService
{
    private readonly AppSettings _settings;
    private readonly CentralDataService _centralDataService;
    private readonly ScriptExecutionService _scriptExecutionService;
    private readonly SyncLogService _logService;

    public WindowsSyncWorker(
        AppSettings settings,
        CentralDataService centralDataService,
        ScriptExecutionService scriptExecutionService,
        SyncLogService logService)
    {
        _settings = settings;
        _centralDataService = centralDataService;
        _scriptExecutionService = scriptExecutionService;
        _logService = logService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SafeWriteLogAsync("SERVICIO", "SERVICIO", "Servicio de sincronizacion iniciado.", "RUNNING", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken);

            var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.WindowsService.IntervalMinutes));
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var mode = ParseExecutionMode();
        var filterCodes = ParseLocalCodes();

        await SafeWriteLogAsync("SERVICIO", "SERVICIO", $"Inicio de ciclo automatico. Modo={mode}.", "RUNNING", cancellationToken);
        await _logService.EnsureTableAsync(cancellationToken);

        var locals = await _centralDataService.LoadTpvsAsync(cancellationToken);
        var selectedLocals = filterCodes.Count == 0
            ? locals
            : locals.Where(x => filterCodes.Contains(x.Codigo)).ToList();

        if (selectedLocals.Count == 0)
        {
            await SafeWriteLogAsync("SERVICIO", "SERVICIO", "No hay locales configurados para ejecutar.", "WARN", cancellationToken);
            return;
        }

        foreach (var tpv in selectedLocals)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Inicio de sincronizacion automatica", "RUNNING", cancellationToken);
                await _scriptExecutionService.ExecuteForLocalAsync(
                    tpv,
                    mode,
                    progress => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {progress.LocalDescripcion} - {progress.Etapa} ({progress.OverallPercent}%)"),
                    message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}"),
                    cancellationToken,
                    "proceso automatico alfa");
                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Sincronizacion automatica OK", "OK", cancellationToken);
            }
            catch (Exception ex)
            {
                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), ex.ToString(), "ERROR", cancellationToken);
            }
        }

        await SafeWriteLogAsync("SERVICIO", "SERVICIO", "Ciclo automatico finalizado.", "OK", cancellationToken);
    }

    private SyncExecutionMode ParseExecutionMode()
    {
        return Enum.TryParse<SyncExecutionMode>(_settings.WindowsService.ExecutionMode, true, out var mode)
            ? mode
            : SyncExecutionMode.PricesAndCosts;
    }

    private HashSet<string> ParseLocalCodes()
    {
        return _settings.WindowsService.LocalCodes
            .Split([',', ';', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task SafeWriteLogAsync(string local, string process, string message, string state, CancellationToken cancellationToken)
    {
        try
        {
            await _logService.WriteAsync(local, process, message, state, cancellationToken);
        }
        catch
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {state} {process}: {message}");
        }
    }
}
