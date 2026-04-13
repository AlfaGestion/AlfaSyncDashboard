using System.Globalization;
using AlfaSyncDashboard.Forms;
using AlfaSyncDashboard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace AlfaSyncDashboard;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-AR");

        if (args.Any(x => string.Equals(x, "--service", StringComparison.OrdinalIgnoreCase)))
        {
            RunAsWindowsService(args);
            return;
        }

        ApplicationConfiguration.Initialize();

        var configService = new AppConfigService();
        var appSettings = configService.Load();

        var centralDataService = new CentralDataService(appSettings);
        var analysisService = new AnalysisService(appSettings);
        var priceControlService = new PriceControlService(appSettings);
        var syncStatisticsService = new SyncStatisticsService(appSettings);
        var scriptExecutorService = new ScriptExecutionService(appSettings, syncStatisticsService);
        var logService = new SyncLogService(appSettings);

        Application.Run(new MainForm(
            configService,
            centralDataService,
            analysisService,
            priceControlService,
            scriptExecutorService,
            logService));
    }

    private static void RunAsWindowsService(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices(services =>
            {
                var configService = new AppConfigService();
                var appSettings = configService.Load();

                services.AddSingleton(configService);
                services.AddSingleton(appSettings);
                services.Configure<WindowsServiceLifetimeOptions>(options =>
                {
                    options.ServiceName = string.IsNullOrWhiteSpace(appSettings.WindowsService.ServiceName)
                        ? "Alfa Sincronizacion PDV Sync"
                        : appSettings.WindowsService.ServiceName.Trim();
                });
                services.AddSingleton<CentralDataService>();
                services.AddSingleton<SyncStatisticsService>();
                services.AddSingleton<ScriptExecutionService>();
                services.AddSingleton<SyncLogService>();
                services.AddHostedService<WindowsSyncWorker>();
            })
            .Build()
            .Run();
    }
}
