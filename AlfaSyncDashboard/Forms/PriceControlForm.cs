using System.Data;
using AlfaSyncDashboard.Models;
using AlfaSyncDashboard.Services;

namespace AlfaSyncDashboard.Forms;

public sealed class PriceControlForm : Form
{
    private const string SalesTipoLista = "V";
    private readonly PriceControlService _service;
    private readonly IReadOnlyList<TpvInfo> _locals;

    private readonly ComboBox _cmbMode = new();
    private readonly NumericUpDown _numLimit = new();
    private readonly TextBox _txtSearch = new();
    private readonly ComboBox _cmbList = new();
    private readonly TextBox _txtTipoLista = new();
    private readonly ComboBox _cmbPriceColumn = new();
    private readonly CheckBox _chkOnlyDifferences = new();
    private readonly Button _btnLoad = new();
    private readonly Label _lblStatus = new();
    private readonly DataGridView _grid = new();
    private PriceControlResult? _lastResult;
    private bool _isLoading;

    public PriceControlForm(PriceControlService service, IReadOnlyList<TpvInfo> locals)
    {
        _service = service;
        _locals = locals;

        Text = "Control de precios";
        Width = 1400;
        Height = 800;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        Shown += async (_, _) => await LoadListsAsync();
        UpdateModeUi();
    }

    private void BuildLayout()
    {
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 10,
            Padding = new Padding(8)
        };

        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        _cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMode.Items.AddRange(["Costo", "Precio de lista"]);
        _cmbMode.SelectedIndex = 0;
        _cmbMode.SelectedIndexChanged += async (_, _) =>
        {
            UpdateModeUi();
            await AutoRefreshAsync();
        };

        _numLimit.Minimum = 1;
        _numLimit.Maximum = 1000;
        _numLimit.Value = 100;

        _txtSearch.PlaceholderText = "Código o descripción";
        _txtSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LoadDataAsync();
            }
        };

        _cmbList.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbList.SelectedIndexChanged += async (_, _) =>
        {
            await AutoRefreshAsync();
        };
        _txtTipoLista.Text = SalesTipoLista;
        _txtTipoLista.ReadOnly = true;

        _cmbPriceColumn.DropDownStyle = ComboBoxStyle.DropDownList;
        for (var i = 1; i <= 8; i++)
            _cmbPriceColumn.Items.Add($"Precio{i}");
        _cmbPriceColumn.SelectedIndex = 0;
        _cmbPriceColumn.SelectedIndexChanged += async (_, _) => await AutoRefreshAsync();

        _chkOnlyDifferences.Text = "Solo con diferencia";
        _chkOnlyDifferences.AutoSize = true;
        _chkOnlyDifferences.CheckedChanged += (_, _) => RebindCurrentResult();

        _btnLoad.Text = "Consultar";
        _btnLoad.AutoSize = true;
        _btnLoad.Click += async (_, _) => await LoadDataAsync();

        top.Controls.Add(new Label { Text = "Modo", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        top.Controls.Add(_cmbMode, 1, 0);
        top.Controls.Add(new Label { Text = "Cantidad", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        top.Controls.Add(_numLimit, 3, 0);
        top.Controls.Add(new Label { Text = "Buscar", AutoSize = true, Anchor = AnchorStyles.Left }, 4, 0);
        top.Controls.Add(_txtSearch, 5, 0);
        top.Controls.Add(new Label { Text = "Lista", AutoSize = true, Anchor = AnchorStyles.Left }, 6, 0);
        top.Controls.Add(_cmbList, 7, 0);
        top.Controls.Add(new Label { Text = "Clase", AutoSize = true, Anchor = AnchorStyles.Left }, 8, 0);
        top.Controls.Add(_cmbPriceColumn, 9, 0);

        top.Controls.Add(new Label { Text = "Tipo lista", AutoSize = true, Anchor = AnchorStyles.Left }, 6, 1);
        top.Controls.Add(_txtTipoLista, 7, 1);
        top.Controls.Add(_chkOnlyDifferences, 8, 1);
        top.Controls.Add(_btnLoad, 9, 1);

        _lblStatus.Dock = DockStyle.Top;
        _lblStatus.Height = 24;
        _lblStatus.Padding = new Padding(8, 0, 8, 0);
        _lblStatus.Text = $"Locales comparados: {string.Join(" | ", _locals.Select(x => x.Descripcion))}";

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        _grid.RowHeadersVisible = false;
        _grid.CellFormatting += GridOnCellFormatting;

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(top);
    }

    private void UpdateModeUi()
    {
        var isPriceMode = _cmbMode.SelectedIndex == 1;
        _cmbList.Enabled = isPriceMode;
        _txtTipoLista.Enabled = isPriceMode;
        _cmbPriceColumn.Enabled = isPriceMode;

        if (!isPriceMode)
        {
            _txtTipoLista.Text = string.Empty;
        }
        else
        {
            _txtTipoLista.Text = SalesTipoLista;
        }
    }

    private async Task LoadListsAsync()
    {
        try
        {
            _isLoading = true;
            var lists = await _service.LoadAvailableListsAsync();
            _cmbList.Items.Clear();
            foreach (var item in lists)
                _cmbList.Items.Add(item);

            if (_cmbList.Items.Count > 0)
                _cmbList.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Control de precios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadDataAsync()
    {
        if (_isLoading)
            return;

        var request = BuildRequest();
        if (request is null)
            return;

        try
        {
            UseWaitCursor = true;
            _btnLoad.Enabled = false;
            _lblStatus.Text = "Consultando datos...";

            var result = await _service.LoadAsync(_locals, request);
            _lastResult = result;
            UpdateGridAndStatus(result);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Error en la consulta.";
            MessageBox.Show(this, ex.Message, "Control de precios", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _btnLoad.Enabled = true;
        }
    }

    private async Task AutoRefreshAsync()
    {
        if (_isLoading)
            return;

        if (_cmbMode.SelectedIndex == 1 && _cmbList.SelectedItem is null)
            return;

        await LoadDataAsync();
    }

    private PriceControlRequest? BuildRequest()
    {
        var request = new PriceControlRequest
        {
            Mode = _cmbMode.SelectedIndex == 0 ? PriceControlMode.Cost : PriceControlMode.PriceList,
            Limit = (int)_numLimit.Value,
            SearchText = _txtSearch.Text.Trim(),
            PriceListId = (_cmbList.SelectedItem as PriceListOption)?.IdLista ?? string.Empty,
            TipoLista = _cmbMode.SelectedIndex == 1 ? SalesTipoLista : string.Empty,
            PriceColumn = _cmbPriceColumn.SelectedIndex + 1
        };

        if (request.Mode == PriceControlMode.PriceList && string.IsNullOrWhiteSpace(request.PriceListId))
        {
            MessageBox.Show(this, "Ingresá la lista para consultar precios.", "Control de precios", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return request;
    }

    private void RebindCurrentResult()
    {
        if (_lastResult is not null)
            UpdateGridAndStatus(_lastResult);
    }

    private void UpdateGridAndStatus(PriceControlResult result)
    {
        var visibleCount = BindResult(result);
        var detail = string.Join(" | ", _locals.Select(local =>
        {
            var key = BuildLocalKey(local);
            return result.LocalErrors.ContainsKey(key)
                ? $"{local.Descripcion}: ERROR"
                : $"{local.Descripcion}: {result.LocalMatches.GetValueOrDefault(key)} encontrados";
        }));

        var filterText = _chkOnlyDifferences.Checked
            ? $" Mostrando con diferencia: {visibleCount}."
            : string.Empty;

        _lblStatus.Text = $"Consulta finalizada. Artículos: {result.Rows.Count}.{filterText} {detail}".Trim();
    }

    private int BindResult(PriceControlResult result)
    {
        var table = new DataTable();
        table.Columns.Add("IDARTICULO", typeof(string));
        table.Columns.Add("DESCRIPCION", typeof(string));
        table.Columns.Add("CENTRAL", typeof(decimal));

        foreach (var local in _locals)
            table.Columns.Add(BuildLocalColumnName(local), typeof(decimal));

        var rowsToShow = _chkOnlyDifferences.Checked
            ? result.Rows.Where(HasDifferences).ToList()
            : result.Rows;

        foreach (var row in rowsToShow)
        {
            var dataRow = table.NewRow();
            dataRow["IDARTICULO"] = row.ArticleId;
            dataRow["DESCRIPCION"] = row.Description;
            dataRow["CENTRAL"] = row.CentralValue.HasValue ? row.CentralValue.Value : DBNull.Value;

            foreach (var local in _locals)
            {
                var key = BuildLocalKey(local);
                var column = BuildLocalColumnName(local);
                dataRow[column] = row.LocalValues.TryGetValue(key, out var value) && value.HasValue
                    ? value.Value
                    : DBNull.Value;
            }

            table.Rows.Add(dataRow);
        }

        _grid.DataSource = table;
        _grid.Columns["IDARTICULO"].HeaderText = "Código";
        _grid.Columns["DESCRIPCION"].HeaderText = "Descripción";
        _grid.Columns["CENTRAL"].HeaderText = result.CentralColumnTitle;
        _grid.Columns["CENTRAL"].DefaultCellStyle.Format = "N4";

        foreach (var local in _locals)
        {
            var key = BuildLocalKey(local);
            var column = BuildLocalColumnName(local);
            _grid.Columns[column].HeaderText = result.LocalErrors.TryGetValue(key, out var error)
                ? $"{local.Descripcion} (ERROR)"
                : $"{local.Descripcion} ({result.LocalMatches.GetValueOrDefault(key)})";
            _grid.Columns[column].DefaultCellStyle.Format = "N4";
        }

        return rowsToShow.Count;
    }

    private void GridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].Name is "IDARTICULO" or "DESCRIPCION" or "CENTRAL")
            return;

        var row = _grid.Rows[e.RowIndex];
        var centralValue = row.Cells["CENTRAL"].Value;
        var localValue = row.Cells[e.ColumnIndex].Value;

        if (centralValue == DBNull.Value || localValue == DBNull.Value)
        {
            e.CellStyle ??= new DataGridViewCellStyle();
            e.CellStyle.BackColor = Color.MistyRose;
            e.CellStyle.ForeColor = Color.DarkRed;
            return;
        }

        var central = Convert.ToDecimal(centralValue);
        var local = Convert.ToDecimal(localValue);
        e.CellStyle ??= new DataGridViewCellStyle();
        if (central == local)
        {
            e.CellStyle.BackColor = Color.Honeydew;
            e.CellStyle.ForeColor = Color.DarkGreen;
        }
        else
        {
            e.CellStyle.BackColor = Color.LightGoldenrodYellow;
            e.CellStyle.ForeColor = Color.DarkRed;
            e.CellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
        }
    }

    private static string BuildLocalKey(TpvInfo local)
        => $"{local.Codigo}|{local.Descripcion}";

    private static string BuildLocalColumnName(TpvInfo local)
        => $"LOCAL_{local.Codigo}";

    private bool HasDifferences(PriceControlRow row)
    {
        foreach (var local in _locals)
        {
            var key = BuildLocalKey(local);
            var localValue = row.LocalValues.TryGetValue(key, out var value) ? value : null;

            if (row.CentralValue != localValue)
                return true;
        }

        return false;
    }
}
