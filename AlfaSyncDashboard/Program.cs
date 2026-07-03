using System.Globalization;
using AlfaSyncDashboard.Forms;
using AlfaSyncDashboard.Models;
using AlfaSyncDashboard.Services;
using System.Windows.Forms;

namespace AlfaSyncDashboard;

internal static class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-AR");

        var configService = new AppConfigService();
        var appSettings = configService.Load();
        var isConsoleMode = TryParseConsoleMode(args, out var syncMode);

        if (string.IsNullOrWhiteSpace(appSettings.CentralConnectionString))
        {
            if (isConsoleMode)
            {
                Console.Error.WriteLine("La configuración central no está completa. Ejecutá la app una vez en modo visual.");
                return 2;
            }

            ApplicationConfiguration.Initialize();
            using var wizard = new SetupWizardForm(appSettings);
            if (wizard.ShowDialog() != DialogResult.OK)
                return 1;
            configService.Save(appSettings);
        }

        var centralDataService = new CentralDataService(appSettings);
        var analysisService = new AnalysisService(appSettings);
        var priceControlService = new PriceControlService(appSettings);
        var scriptExecutorService = new ScriptExecutionService(appSettings);
        var localPendingSyncService = new LocalPendingSyncService(appSettings);
        var logService = new SyncLogService(appSettings);
        var localFileLogService = new LocalFileLogService();
        var lockService = new ExecutionLockService();
        var syncRunnerService = new SyncRunnerService(localPendingSyncService, scriptExecutorService, logService, lockService);
        var scheduledTaskService = new ScheduledTaskService();

        if (isConsoleMode)
            return await RunConsoleSyncAsync(centralDataService, syncRunnerService, syncMode, localFileLogService);

        ApplicationConfiguration.Initialize();

        Application.Run(new MainForm(
            configService,
            centralDataService,
            analysisService,
            priceControlService,
            scriptExecutorService,
            logService,
            syncRunnerService,
            scheduledTaskService,
            localFileLogService));

        return 0;
    }

    private static bool TryParseConsoleMode(string[] args, out SyncExecutionMode syncMode)
    {
        syncMode = SyncExecutionMode.PricesAndCosts;
        if (args.Length < 2 || !args[0].Equals("--sync", StringComparison.OrdinalIgnoreCase))
            return false;

        syncMode = args[1].Equals("full", StringComparison.OrdinalIgnoreCase)
            ? SyncExecutionMode.Full
            : SyncExecutionMode.PricesAndCosts;

        return true;
    }

    private static async Task<int> RunConsoleSyncAsync(
        CentralDataService centralDataService,
        SyncRunnerService syncRunnerService,
        SyncExecutionMode syncMode,
        LocalFileLogService localFileLogService)
    {
        try
        {
            var tpvs = await centralDataService.LoadTpvsAsync();
            var selected = tpvs.Where(x => x.Selected).ToList();
            if (selected.Count == 0)
            {
                const string message = "No hay locales seleccionados en la configuración.";
                Console.WriteLine(message);
                localFileLogService.Write(message);
                return 3;
            }

            var startMessage = $"Iniciando sincronización {syncMode} para {selected.Count} locales.";
            Console.WriteLine(startMessage);
            localFileLogService.Write(startMessage);
            await syncRunnerService.RunAsync(
                selected,
                syncMode,
                progress =>
                {
                    var line = $"{progress.LocalDescripcion} | {progress.Etapa} | {progress.OverallPercent}%";
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} {line}");
                    localFileLogService.Write(line);
                },
                message =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");
                    localFileLogService.Write(message);
                },
                (tpv, state) =>
                {
                    var line = $"[{tpv.Descripcion}] {state}";
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} {line}");
                    localFileLogService.Write(line);
                },
                CancellationToken.None);

            Console.WriteLine("Sincronización finalizada.");
            localFileLogService.Write("Sincronización finalizada.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            localFileLogService.Write($"ERROR: {ex}");
            return 1;
        }
    }
}
