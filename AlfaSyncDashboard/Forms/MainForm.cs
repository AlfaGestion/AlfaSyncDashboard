using AlfaSyncDashboard.Models;
using AlfaSyncDashboard.Services;
using System.ComponentModel;
using System.Text;

namespace AlfaSyncDashboard.Forms;

public sealed class MainForm : Form
{
    private readonly AppConfigService _configService;
    private readonly CentralDataService _centralDataService;
    private readonly AnalysisService _analysisService;
    private readonly PriceControlService _priceControlService;
    private readonly ScriptExecutionService _scriptExecutionService;
    private readonly SyncLogService _logService;
    private AppSettings _settings;

    private readonly BindingList<TpvInfo> _tpvs = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _txtLog = new();
    private readonly ProgressBar _progressGeneral = new();
    private readonly ProgressBar _progressLocal = new();
    private readonly Label _lblStatus = new();
    private readonly Label _lblTimers = new();
    private CancellationTokenSource? _cts;

    public MainForm(
        AppConfigService configService,
        CentralDataService centralDataService,
        AnalysisService analysisService,
        PriceControlService priceControlService,
        ScriptExecutionService scriptExecutionService,
        SyncLogService logService)
    {
        _configService = configService;
        _centralDataService = centralDataService;
        _analysisService = analysisService;
        _priceControlService = priceControlService;
        _scriptExecutionService = scriptExecutionService;
        _logService = logService;
        _settings = configService.Load();

        Text = "Alfa Sync Dashboard";
        Width = 1500;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        Shown += async (_, _) => await InitializeAsync();
    }

    private void BuildLayout()
    {
        var topButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        Button btnRefresh = NewButton("Actualizar locales", async (_, _) => await LoadTpvsAsync());
        Button btnSettings = NewButton("Configuración", (_, _) => ShowSettings());
        Button btnConnections = NewButton("Probar conexiones", async (_, _) => await TestConnectionsAsync());
        Button btnAnalyze = NewButton("Analizar seleccionados", async (_, _) => await AnalyzeSelectedAsync());
        Button btnControl = NewButton("Control precios", (_, _) => ShowPriceControl());
        Button btnPrices = NewButton("Enviar precios y costos", async (_, _) => await ExecuteSelectedAsync(SyncExecutionMode.PricesAndCosts));
        Button btnFull = NewButton("Enviar todo", async (_, _) => await ExecuteSelectedAsync(SyncExecutionMode.Full));
        Button btnCancel = NewButton("Cancelar", (_, _) => CancelCurrent());

        topButtons.Controls.AddRange([btnRefresh, btnSettings, btnConnections, btnAnalyze, btnControl, btnPrices, btnFull, btnCancel]);

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.DataSource = _tpvs;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(TpvInfo.Selected), HeaderText = "Sel", Width = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.Codigo), HeaderText = "Código", Width = 70, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.Sucursal), HeaderText = "Sucursal", Width = 80, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.Descripcion), HeaderText = "Descripción", Width = 220, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.Server), HeaderText = "Server", Width = 190, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.DbName), HeaderText = "Base", Width = 140, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.ScriptSet), HeaderText = "ScriptSet", Width = 100, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.EstadoConexion), HeaderText = "Conexión", Width = 110, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.UltimaSincronizacion), HeaderText = "Última sync", Width = 145, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TpvInfo.EstadoActual), HeaderText = "Estado", Width = 280, ReadOnly = true });

        _txtLog.Dock = DockStyle.Fill;
        _txtLog.Multiline = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.ReadOnly = true;
        _txtLog.Font = new Font("Consolas", 10);

        _progressGeneral.Dock = DockStyle.Top;
        _progressGeneral.Height = 22;
        _progressLocal.Dock = DockStyle.Top;
        _progressLocal.Height = 22;

        _lblStatus.Dock = DockStyle.Top;
        _lblStatus.Height = 22;
        _lblStatus.Text = "Listo";
        _lblTimers.Dock = DockStyle.Top;
        _lblTimers.Height = 22;

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 420 };
        split.Panel1.Controls.Add(_grid);
        split.Panel2.Controls.Add(_txtLog);
        split.Panel2.Controls.Add(_lblTimers);
        split.Panel2.Controls.Add(_lblStatus);
        split.Panel2.Controls.Add(_progressLocal);
        split.Panel2.Controls.Add(_progressGeneral);

        Controls.Add(split);
        Controls.Add(topButtons);
    }

    private Button NewButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 28 };
        button.Click += onClick;
        return button;
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _logService.EnsureTableAsync();
            await LoadTpvsAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"Error inicializando: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Inicialización", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadTpvsAsync()
    {
        try
        {
            SetStatus("Cargando locales...");
            _tpvs.Clear();
            var data = await _centralDataService.LoadTpvsAsync();
            foreach (var tpv in data) _tpvs.Add(tpv);
            AppendLog($"Se cargaron {_tpvs.Count} locales desde V_TA_TPV.");
            SetStatus("Locales cargados.");
        }
        catch (Exception ex)
        {
            AppendLog($"Error cargando locales: {ex.Message}");
            SetStatus("Error cargando locales.");
        }
    }

    private async Task TestConnectionsAsync()
    {
        foreach (var tpv in _tpvs)
        {
            try
            {
                tpv.EstadoConexion = await _analysisService.TestLocalConnectionAsync(tpv) ? "OK" : "ERROR";
            }
            catch (Exception ex)
            {
                tpv.EstadoConexion = "ERROR";
                AppendLog($"[{tpv.Descripcion}] Conexión fallida: {ex.Message}");
            }
            _grid.Refresh();
        }
    }

    private async Task AnalyzeSelectedAsync()
    {
        var selected = _tpvs.Where(x => x.Selected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Seleccioná al menos un local.", "Analizar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus("Analizando diferencias...");
        foreach (var tpv in selected)
        {
            try
            {
                tpv.EstadoActual = "Analizando...";
                _grid.Refresh();
                var analysis = await _analysisService.AnalyzeAsync(tpv);
                tpv.EstadoActual = analysis.ToString();
                AppendLog($"[{tpv.Descripcion}] {analysis}");
                await _logService.WriteAsync(tpv.Descripcion, "ANALISIS", analysis.ToString(), "OK");
            }
            catch (Exception ex)
            {
                tpv.EstadoActual = "Error en análisis";
                AppendLog($"[{tpv.Descripcion}] Error analizando: {ex.Message}");
                await _logService.WriteAsync(tpv.Descripcion, "ANALISIS", ex.ToString(), "ERROR");
            }
            _grid.Refresh();
        }
        SetStatus("Análisis finalizado.");
    }

    private async Task ExecuteSelectedAsync(SyncExecutionMode mode)
    {
        var selected = _tpvs.Where(x => x.Selected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Seleccioná al menos un local.", "Sincronizar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _cts = new CancellationTokenSource();
        _progressGeneral.Value = 0;
        _progressLocal.Value = 0;
        var started = DateTime.Now;

        for (int i = 0; i < selected.Count; i++)
        {
            var tpv = selected[i];
            try
            {
                _progressLocal.Value = 0;
                tpv.EstadoActual = "Sincronizando...";
                _grid.Refresh();

                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Inicio de sincronización", "RUNNING", _cts.Token);
                await _scriptExecutionService.ExecuteForLocalAsync(
                    tpv,
                    mode,
                    progress =>
                    {
                        SafeUi(() =>
                        {
                            _progressLocal.Value = Math.Clamp(progress.OverallPercent, 0, 100);
                            var generalPercent = ((i + (progress.OverallPercent / 100d)) / selected.Count) * 100d;
                            _progressGeneral.Value = Math.Clamp((int)Math.Round(generalPercent), 0, 100);
                            _lblStatus.Text = $"{progress.LocalDescripcion} - {progress.Etapa} ({progress.OverallPercent}%)";
                            _lblTimers.Text = $"Transcurrido: {progress.Elapsed:hh\\:mm\\:ss} | Restante estimado etapa: {progress.EstimatedRemaining:hh\\:mm\\:ss}";
                        });
                    },
                    AppendLog,
                    _cts.Token,
                    Environment.UserName);

                tpv.EstadoActual = "OK";
                tpv.UltimaSincronizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Sincronización OK", "OK", _cts.Token);
                AppendLog($"[{tpv.Descripcion}] sincronización finalizada correctamente.");
            }
            catch (OperationCanceledException)
            {
                tpv.EstadoActual = "Cancelado";
                SafeUi(() => _progressLocal.Value = 0);
                AppendLog($"[{tpv.Descripcion}] proceso cancelado por el usuario.");
                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), "Cancelado por el usuario", "CANCEL", CancellationToken.None);
                break;
            }
            catch (Exception ex)
            {
                tpv.EstadoActual = "ERROR";
                SafeUi(() => _progressLocal.Value = 0);
                AppendLog($"[{tpv.Descripcion}] error: {ex.Message}");
                await _logService.WriteAsync(tpv.Descripcion, mode.ToString(), ex.ToString(), "ERROR", CancellationToken.None);
            }
            finally
            {
                _grid.Refresh();
            }
        }

        SetStatus($"Proceso terminado. Duración total: {(DateTime.Now - started):hh\\:mm\\:ss}");
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _settings = form.Settings;
            _configService.Save(_settings);
            MessageBox.Show(this, "Configuración guardada. Reiniciá la app para recargar servicios o volvé a abrirla.", "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ShowPriceControl()
    {
        var selected = _tpvs.Where(x => x.Selected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Seleccioná al menos un local para abrir el control de precios.", "Control de precios", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new PriceControlForm(_priceControlService, selected);
        form.ShowDialog(this);
    }

    private void CancelCurrent()
    {
        _cts?.Cancel();
    }

    private void AppendLog(string message)
    {
        SafeUi(() =>
        {
            _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        });
    }

    private void SetStatus(string text)
    {
        SafeUi(() => _lblStatus.Text = text);
    }

    private void SafeUi(Action action)
    {
        if (InvokeRequired)
            BeginInvoke(action);
        else
            action();
    }
}
