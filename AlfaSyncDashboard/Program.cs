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
        var logService = new SyncLogService(appSettings);
        var lockService = new ExecutionLockService();
        var syncRunnerService = new SyncRunnerService(scriptExecutorService, logService, lockService);
        var scheduledTaskService = new ScheduledTaskService();

        if (isConsoleMode)
            return await RunConsoleSyncAsync(centralDataService, syncRunnerService, syncMode);

        ApplicationConfiguration.Initialize();

        Application.Run(new MainForm(
            configService,
            centralDataService,
            analysisService,
            priceControlService,
            scriptExecutorService,
            logService,
            syncRunnerService,
            scheduledTaskService));

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
        SyncExecutionMode syncMode)
    {
        try
        {
            var tpvs = await centralDataService.LoadTpvsAsync();
            var selected = tpvs.Where(x => x.Selected).ToList();
            if (selected.Count == 0)
            {
                Console.WriteLine("No hay locales seleccionados en la configuración.");
                return 3;
            }

            Console.WriteLine($"Iniciando sincronización {syncMode} para {selected.Count} locales.");
            await syncRunnerService.RunAsync(
                selected,
                syncMode,
                progress =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} {progress.LocalDescripcion} | {progress.Etapa} | {progress.OverallPercent}%");
                },
                message => Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}"),
                (tpv, state) => Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{tpv.Descripcion}] {state}"),
                CancellationToken.None);

            Console.WriteLine("Sincronización finalizada.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
