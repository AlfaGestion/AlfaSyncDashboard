using AlfaSyncDashboard.Models;
using AlfaSyncDashboard.Services;
using System.Diagnostics;
using System.Security.Principal;

namespace AlfaSyncDashboard.Forms;

public sealed class SettingsForm : Form
{
    private readonly SyncStatisticsService _syncStatisticsService;
    private readonly TextBox _txtScriptsPath = new() { Dock = DockStyle.Top };
    private readonly TextBox _txtCentralConnection = new() { Dock = DockStyle.Top, Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 60 };
    private readonly TextBox _txtSyncApiClientId = new() { Dock = DockStyle.Top };
    private readonly TextBox _txtServiceName = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _numIntervalMinutes = new() { Minimum = 1, Maximum = 1440, Width = 100 };
    private readonly ComboBox _cmbExecutionMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly CheckedListBox _lstLocals = new() { Dock = DockStyle.Fill, CheckOnClick = true, Height = 140, IntegralHeight = false };
    private readonly Button _btnCheckAll = new() { Text = "Tildar todas", AutoSize = true };
    private readonly Button _btnUncheckAll = new() { Text = "Destildar todas", AutoSize = true };
    private readonly Label _lblLocalsStatus = new() { Text = "Cargando sucursales...", AutoSize = true };
    private readonly Button _btnInstallService = new() { Text = "Instalar/Actualizar servicio", AutoSize = true };
    private readonly Button _btnStartService = new() { Text = "Iniciar servicio", AutoSize = true };
    private readonly Button _btnStopService = new() { Text = "Detener servicio", AutoSize = true };
    private readonly Label _lblServiceStatus = new() { Text = "Estado del servicio: no consultado", AutoSize = true };

    public AppSettings Settings { get; }

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;
        _syncStatisticsService = new SyncStatisticsService(settings);
        Text = "Configuracion";
        Width = 900;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        var btnBrowse = new Button { Text = "Seleccionar carpeta...", Dock = DockStyle.Right, Width = 160 };
        btnBrowse.Click += (_, _) => BrowseFolder();

        var pathPanel = new Panel { Dock = DockStyle.Top, Height = 34 };
        pathPanel.Controls.Add(_txtScriptsPath);
        pathPanel.Controls.Add(btnBrowse);

        var servicePanel = BuildServicePanel();

        var btnSave = new Button { Text = "Guardar", Dock = DockStyle.Right, Width = 120 };
        btnSave.Click += (_, _) => SaveAndClose();
        var btnCancel = new Button { Text = "Cancelar", Dock = DockStyle.Right, Width = 120 };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var serviceActionsBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        serviceActionsBottom.Controls.Add(_btnInstallService);
        serviceActionsBottom.Controls.Add(_btnStartService);
        serviceActionsBottom.Controls.Add(_btnStopService);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 44 };
        bottom.Controls.Add(serviceActionsBottom);
        bottom.Controls.Add(btnSave);
        bottom.Controls.Add(btnCancel);

        var lbl1 = new Label { Text = "Ruta base de scripts", Dock = DockStyle.Top, Height = 20 };
        var lbl2 = new Label { Text = "Connection string central", Dock = DockStyle.Top, Height = 20 };
        var lbl3 = new Label { Text = "Id cliente API", Dock = DockStyle.Top, Height = 20 };

        Controls.Add(servicePanel);
        var syncApiPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
        syncApiPanel.Controls.Add(_txtSyncApiClientId);
        syncApiPanel.Controls.Add(lbl3);

        Controls.Add(syncApiPanel);
        var connectionPanel = new Panel { Dock = DockStyle.Top, Height = 84 };
        connectionPanel.Controls.Add(_txtCentralConnection);
        connectionPanel.Controls.Add(lbl2);

        Controls.Add(connectionPanel);
        Controls.Add(pathPanel);
        Controls.Add(lbl1);
        Controls.Add(bottom);

        _cmbExecutionMode.Items.AddRange(
        [
            new ExecutionModeItem(SyncExecutionMode.PricesAndCosts, "Enviar precios y costos"),
            new ExecutionModeItem(SyncExecutionMode.Full, "Enviar todo")
        ]);

        _btnCheckAll.Click += (_, _) => SetAllLocalsChecked(true);
        _btnUncheckAll.Click += (_, _) => SetAllLocalsChecked(false);
        _btnInstallService.Click += async (_, _) => await InstallOrUpdateServiceAsync();
        _btnStartService.Click += async (_, _) => await StartServiceAsync();
        _btnStopService.Click += async (_, _) => await StopServiceAsync();

        _txtScriptsPath.Text = settings.DefaultScriptsPath;
        _txtCentralConnection.Text = settings.CentralConnectionString;
        _txtSyncApiClientId.Text = settings.SyncStatistics.IdCliente;
        _txtServiceName.Text = settings.WindowsService.ServiceName;
        _numIntervalMinutes.Value = Math.Clamp(settings.WindowsService.IntervalMinutes, 1, 1440);
        SelectExecutionMode(settings.WindowsService.ExecutionMode);

        Shown += async (_, _) =>
        {
            await LoadLocalsAsync();
            await LoadClientIdAsync();
            await RefreshServiceStatusAsync();
        };
    }

    private GroupBox BuildServicePanel()
    {
        var group = new GroupBox
        {
            Text = "Servicio Windows",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var localButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        localButtons.Controls.Add(_btnCheckAll);
        localButtons.Controls.Add(_btnUncheckAll);

        var localsPanel = new Panel { Dock = DockStyle.Fill };
        _lblLocalsStatus.Dock = DockStyle.Top;
        localsPanel.Controls.Add(_lstLocals);
        localsPanel.Controls.Add(_lblLocalsStatus);

        table.Controls.Add(new Label { Text = "Nombre del servicio", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        table.Controls.Add(_txtServiceName, 1, 0);
        table.Controls.Add(new Label { Text = "Cada minutos", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        table.Controls.Add(_numIntervalMinutes, 1, 1);
        table.Controls.Add(new Label { Text = "Que ejecuta", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        table.Controls.Add(_cmbExecutionMode, 1, 2);
        table.Controls.Add(new Label { Text = "Estado actual", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        table.Controls.Add(_lblServiceStatus, 1, 3);
        table.Controls.Add(new Label { Text = "Sucursales", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
        table.Controls.Add(localButtons, 1, 4);
        table.Controls.Add(localsPanel, 1, 5);

        group.Controls.Add(table);
        return group;
    }

    private async Task LoadLocalsAsync()
    {
        _lstLocals.Items.Clear();
        _lblLocalsStatus.Text = "Cargando sucursales...";

        try
        {
            var tempSettings = new AppSettings
            {
                CentralConnectionString = _txtCentralConnection.Text.Trim(),
                DefaultScriptsPath = Settings.DefaultScriptsPath,
                MaxParallelLocalTasks = Settings.MaxParallelLocalTasks,
                ConnectionTimeoutSeconds = Settings.ConnectionTimeoutSeconds,
                CommandTimeoutSeconds = Settings.CommandTimeoutSeconds,
                LocalScriptMappings = Settings.LocalScriptMappings,
                ScriptSets = Settings.ScriptSets,
                WindowsService = Settings.WindowsService
            };

            var service = new CentralDataService(tempSettings);
            var locals = await service.LoadTpvsAsync();
            var selectedCodes = ParseSelectedCodes(Settings.WindowsService.LocalCodes);

            foreach (var local in locals)
            {
                var item = new LocalSelectionItem(local.Codigo, local.Descripcion);
                var isChecked = selectedCodes.Count == 0 || selectedCodes.Contains(local.Codigo);
                _lstLocals.Items.Add(item, isChecked);
            }

            _lblLocalsStatus.Text = $"{_lstLocals.Items.Count} sucursales cargadas.";
        }
        catch (Exception ex)
        {
            _lblLocalsStatus.Text = $"Error cargando sucursales: {ex.Message}";
        }
    }

    private async Task LoadClientIdAsync()
    {
        try
        {
            var clientId = await _syncStatisticsService.ResolveClientIdAsync();
            if (!string.IsNullOrWhiteSpace(clientId))
                _txtSyncApiClientId.Text = clientId;
        }
        catch
        {
        }
    }

    private static HashSet<string> ParseSelectedCodes(string value)
    {
        return value
            .Split([',', ';', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void SetAllLocalsChecked(bool isChecked)
    {
        for (var i = 0; i < _lstLocals.Items.Count; i++)
            _lstLocals.SetItemChecked(i, isChecked);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog();
        dialog.SelectedPath = _txtScriptsPath.Text;
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _txtScriptsPath.Text = dialog.SelectedPath;
    }

    private void SaveAndClose()
    {
        PersistSettings();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void PersistSettings()
    {
        Settings.DefaultScriptsPath = _txtScriptsPath.Text.Trim();
        Settings.CentralConnectionString = _txtCentralConnection.Text.Trim();
        Settings.SyncStatistics.IdCliente = _txtSyncApiClientId.Text.Trim();
        Settings.WindowsService.ServiceName = GetServiceName();
        Settings.WindowsService.IntervalMinutes = (int)_numIntervalMinutes.Value;
        Settings.WindowsService.ExecutionMode = (_cmbExecutionMode.SelectedItem as ExecutionModeItem)?.Mode.ToString()
            ?? nameof(SyncExecutionMode.PricesAndCosts);
        Settings.WindowsService.LocalCodes = string.Join(",",
            _lstLocals.CheckedItems
                .OfType<LocalSelectionItem>()
                .Select(x => x.Code));

        new AppConfigService().Save(Settings);
    }

    private string GetServiceName()
    {
        return string.IsNullOrWhiteSpace(_txtServiceName.Text)
            ? "Alfa Sincronizacion PDV Sync"
            : _txtServiceName.Text.Trim();
    }

    private async Task InstallOrUpdateServiceAsync()
    {
        try
        {
            PersistSettings();
            var serviceName = GetServiceName();
            var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            var binPath = $"\\\"{exePath}\\\" --service";

            if (!IsRunningAsAdministrator())
            {
                if (PromptForElevation("instalar o actualizar el servicio"))
                    await RunElevatedAsync(
                        "powershell.exe",
                        $"-NoProfile -Command \"if ((sc.exe query \\\"{serviceName}\\\" 2>$null | Out-String) -match 'SERVICE_NAME') " +
                        $"{{ sc.exe config \\\"{serviceName}\\\" binPath= '\\\"{binPath}\\\"' DisplayName= '\\\"{serviceName}\\\"' start= auto }} " +
                        $"else {{ sc.exe create \\\"{serviceName}\\\" binPath= '\\\"{binPath}\\\"' DisplayName= '\\\"{serviceName}\\\"' start= auto }}; " +
                        $"sc.exe start \\\"{serviceName}\\\"\"");
                return;
            }

            var exists = await RunScAsync($"query \"{serviceName}\"", throwOnError: false);
            if (exists.ExitCode == 0)
            {
                await RunScAsync($"config \"{serviceName}\" binPath= \"{binPath}\" DisplayName= \"{serviceName}\" start= auto");
            }
            else
            {
                await RunScAsync($"create \"{serviceName}\" binPath= \"{binPath}\" DisplayName= \"{serviceName}\" start= auto");
            }

            await RunScAsync($"start \"{serviceName}\"", throwOnError: false);
            await RefreshServiceStatusAsync();
            MessageBox.Show(this, $"Servicio '{serviceName}' instalado/actualizado e iniciado.", "Servicio Windows", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Servicio Windows", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task StartServiceAsync()
    {
        try
        {
            PersistSettings();
            var serviceName = GetServiceName();

            if (!IsRunningAsAdministrator())
            {
                if (PromptForElevation("iniciar el servicio"))
                    await RunElevatedAsync("sc.exe", $"start \"{serviceName}\"");
                return;
            }

            await RunScAsync($"start \"{serviceName}\"");
            await RefreshServiceStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Servicio Windows", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task StopServiceAsync()
    {
        try
        {
            var serviceName = GetServiceName();

            if (!IsRunningAsAdministrator())
            {
                if (PromptForElevation("detener el servicio"))
                    await RunElevatedAsync("sc.exe", $"stop \"{serviceName}\"");
                return;
            }

            await RunScAsync($"stop \"{serviceName}\"");
            await RefreshServiceStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Servicio Windows", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshServiceStatusAsync()
    {
        var serviceName = GetServiceName();
        var result = await RunScAsync($"query \"{serviceName}\"", throwOnError: false);
        if (result.ExitCode != 0)
        {
            _lblServiceStatus.Text = "Estado del servicio: no instalado";
            return;
        }

        var stateLine = result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.Contains("STATE", StringComparison.OrdinalIgnoreCase));
        _lblServiceStatus.Text = string.IsNullOrWhiteSpace(stateLine)
            ? "Estado del servicio: instalado"
            : $"Estado del servicio: {stateLine.Trim()}";
    }

    private static async Task<(int ExitCode, string Output)> RunScAsync(string arguments, bool throwOnError = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo ejecutar sc.exe.");
        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = string.IsNullOrWhiteSpace(stdErr) ? stdOut : $"{stdOut}{Environment.NewLine}{stdErr}".Trim();
        if (throwOnError && process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(output) ? "Fallo ejecutando sc.exe." : output);

        return (process.ExitCode, output);
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private bool PromptForElevation(string actionDescription)
    {
        return MessageBox.Show(
            this,
            $"Esta accion necesita permisos de administrador para {actionDescription}.{Environment.NewLine}{Environment.NewLine}Queres ejecutarla como administrador?",
            "Servicio Windows",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private async Task RunElevatedAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas"
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo solicitar elevacion.");
        await process.WaitForExitAsync();
        await RefreshServiceStatusAsync();
    }

    private void SelectExecutionMode(string executionMode)
    {
        var parsed = Enum.TryParse<SyncExecutionMode>(executionMode, true, out var mode)
            ? mode
            : SyncExecutionMode.PricesAndCosts;

        foreach (var item in _cmbExecutionMode.Items.OfType<ExecutionModeItem>())
        {
            if (item.Mode == parsed)
            {
                _cmbExecutionMode.SelectedItem = item;
                return;
            }
        }

        _cmbExecutionMode.SelectedIndex = 0;
    }

    private sealed record LocalSelectionItem(string Code, string Description)
    {
        public override string ToString() => $"{Code} - {Description}";
    }

    private sealed record ExecutionModeItem(SyncExecutionMode Mode, string Label)
    {
        public override string ToString() => Label;
    }
}
