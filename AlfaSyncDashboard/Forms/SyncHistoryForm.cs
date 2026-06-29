using AlfaSyncDashboard.Models;
using AlfaSyncDashboard.Services;

namespace AlfaSyncDashboard.Forms;

public sealed class SyncHistoryForm : Form
{
    private readonly SyncLogService _logService;
    private readonly ScheduledTaskService _taskService;

    private readonly DataGridView _grid = new();
    private readonly NumericUpDown _numDays = new();
    private readonly NumericUpDown _numMinutes = new();
    private readonly ComboBox _cmbMode = new();
    private readonly TextBox _txtCommand = new();
    private readonly TextBox _txtTaskInfo = new();
    private readonly Label _lblStatus = new();

    public SyncHistoryForm(SyncLogService logService, ScheduledTaskService taskService)
    {
        _logService = logService;
        _taskService = taskService;

        Text = "Historial y automatización";
        Width = 1400;
        Height = 820;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        Shown += async (_, _) =>
        {
            RefreshCommand();
            await LoadHistoryAsync();
            await LoadTaskInfoAsync();
        };
    }

    private void BuildLayout()
    {
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
            ColumnCount = 8
        };

        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _numDays.Minimum = 1;
        _numDays.Maximum = 365;
        _numDays.Value = 30;

        _numMinutes.Minimum = 5;
        _numMinutes.Maximum = 1440;
        _numMinutes.Value = 15;
        _numMinutes.ValueChanged += (_, _) => RefreshCommand();

        _cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMode.Items.AddRange(["Precios y costos", "Enviar todo"]);
        _cmbMode.SelectedIndex = 0;
        _cmbMode.SelectedIndexChanged += (_, _) => RefreshCommand();

        var btnHistory = new Button { Text = "Actualizar historial", AutoSize = true };
        btnHistory.Click += async (_, _) => await LoadHistoryAsync();

        var btnCreateTask = new Button { Text = "Crear/actualizar tarea", AutoSize = true };
        btnCreateTask.Click += async (_, _) => await CreateTaskAsync();

        var btnQueryTask = new Button { Text = "Ver tarea", AutoSize = true };
        btnQueryTask.Click += async (_, _) => await LoadTaskInfoAsync();

        var btnDeleteTask = new Button { Text = "Borrar tarea", AutoSize = true };
        btnDeleteTask.Click += async (_, _) => await DeleteTaskAsync();

        var btnCopy = new Button { Text = "Copiar comando", AutoSize = true };
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(_txtCommand.Text);
            _lblStatus.Text = "Comando copiado al portapapeles.";
        };

        top.Controls.Add(new Label { Text = "Historial días", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        top.Controls.Add(_numDays, 1, 0);
        top.Controls.Add(btnHistory, 2, 0);
        top.Controls.Add(new Label { Text = "Cada minutos", AutoSize = true, Anchor = AnchorStyles.Left }, 3, 0);
        top.Controls.Add(_numMinutes, 4, 0);
        top.Controls.Add(new Label { Text = "Modo", AutoSize = true, Anchor = AnchorStyles.Left }, 5, 0);
        top.Controls.Add(_cmbMode, 6, 0);
        top.Controls.Add(btnCreateTask, 7, 0);

        var commandPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(8, 0, 8, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        commandPanel.Controls.AddRange([btnQueryTask, btnDeleteTask, btnCopy]);

        _txtCommand.Dock = DockStyle.Top;
        _txtCommand.ReadOnly = true;
        _txtCommand.Height = 60;
        _txtCommand.Multiline = true;
        _txtCommand.Font = new Font("Consolas", 10);

        _txtTaskInfo.Dock = DockStyle.Top;
        _txtTaskInfo.ReadOnly = true;
        _txtTaskInfo.Height = 140;
        _txtTaskInfo.Multiline = true;
        _txtTaskInfo.ScrollBars = ScrollBars.Vertical;
        _txtTaskInfo.Font = new Font("Consolas", 9);

        _lblStatus.Dock = DockStyle.Top;
        _lblStatus.Height = 24;
        _lblStatus.Padding = new Padding(8, 0, 8, 0);
        _lblStatus.Text = "Listo";

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(_txtTaskInfo);
        Controls.Add(_txtCommand);
        Controls.Add(commandPanel);
        Controls.Add(top);
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            _lblStatus.Text = "Cargando historial...";
            var entries = await _logService.GetRecentAsync((int)_numDays.Value);
            _grid.DataSource = entries;
            if (_grid.Columns.Count > 0)
            {
                _grid.Columns[nameof(SyncLogEntry.Fecha)].HeaderText = "Fecha";
                _grid.Columns[nameof(SyncLogEntry.Local)].HeaderText = "Local";
                _grid.Columns[nameof(SyncLogEntry.Proceso)].HeaderText = "Proceso";
                _grid.Columns[nameof(SyncLogEntry.Estado)].HeaderText = "Estado";
                _grid.Columns[nameof(SyncLogEntry.Mensaje)].HeaderText = "Mensaje";
                _grid.Columns[nameof(SyncLogEntry.Mensaje)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _grid.Columns[nameof(SyncLogEntry.Fecha)].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
            }
            _lblStatus.Text = $"Historial cargado: {entries.Count} registros.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error cargando historial.";
            MessageBox.Show(this, ex.Message, "Historial", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CreateTaskAsync()
    {
        try
        {
            _lblStatus.Text = "Creando tarea programada...";
            var output = await _taskService.CreateOrUpdateAsync((int)_numMinutes.Value, GetMode());
            _txtTaskInfo.Text = output;
            _lblStatus.Text = "Tarea programada creada/actualizada.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error creando tarea.";
            MessageBox.Show(this, ex.Message, "Tarea programada", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadTaskInfoAsync()
    {
        try
        {
            _txtTaskInfo.Text = await _taskService.QueryAsync();
            _lblStatus.Text = "Consulta de tarea actualizada.";
        }
        catch (Exception ex)
        {
            _txtTaskInfo.Text = ex.Message;
            _lblStatus.Text = "No se pudo consultar la tarea.";
        }
    }

    private async Task DeleteTaskAsync()
    {
        try
        {
            _txtTaskInfo.Text = await _taskService.DeleteAsync();
            _lblStatus.Text = "Tarea programada eliminada.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error borrando tarea.";
            MessageBox.Show(this, ex.Message, "Tarea programada", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshCommand()
    {
        _txtCommand.Text = _taskService.BuildCreateCommand((int)_numMinutes.Value, GetMode());
    }

    private SyncExecutionMode GetMode()
        => _cmbMode.SelectedIndex == 1 ? SyncExecutionMode.Full : SyncExecutionMode.PricesAndCosts;
}
