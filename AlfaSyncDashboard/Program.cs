using System.Globalization;
using AlfaSyncDashboard.Forms;
using AlfaSyncDashboard.Services;
using System.Windows.Forms;

namespace AlfaSyncDashboard;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-AR");

        var configService = new AppConfigService();
        var appSettings = configService.Load();

        if (string.IsNullOrWhiteSpace(appSettings.CentralConnectionString))
        {
            using var wizard = new SetupWizardForm(appSettings);
            if (wizard.ShowDialog() != DialogResult.OK)
                return;
            configService.Save(appSettings);
        }

        var centralDataService = new CentralDataService(appSettings);
        var analysisService = new AnalysisService(appSettings);
        var priceControlService = new PriceControlService(appSettings);
        var scriptExecutorService = new ScriptExecutionService(appSettings);
        var logService = new SyncLogService(appSettings);

        Application.Run(new MainForm(
            configService,
            centralDataService,
            analysisService,
            priceControlService,
            scriptExecutorService,
            logService));
    }
}
