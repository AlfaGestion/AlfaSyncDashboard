using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Forms;

public sealed class SetupWizardForm : Form
{
    private readonly AppSettings _settings;
    private int _currentStep;
    private bool _connectionVerified;

    // Step 2 — connection
    private readonly TextBox _txtServer = new();
    private readonly TextBox _txtDatabase = new();
    private readonly TextBox _txtUser = new();
    private readonly TextBox _txtPassword = new() { UseSystemPasswordChar = true };
    private readonly CheckBox _chkTrustCert = new() { Text = "Confiar en certificado del servidor", Checked = true };
    private readonly CheckBox _chkEncrypt = new() { Text = "Cifrar conexión", Checked = false };
    private readonly Label _lblConnStatus = new() { AutoSize = false };

    // Step 3 — scripts
    private readonly TextBox _txtScriptsPath = new();
    private readonly Label _lblScriptsStatus = new() { AutoSize = false };

    // Step 4 — summary
    private readonly Label _lblSummary = new() { AutoSize = false };

    // Navigation
    private readonly Button _btnBack = new() { Text = "< Anterior", Width = 110, Height = 30 };
    private readonly Button _btnNext = new() { Text = "Siguiente >", Width = 110, Height = 30 };

    private readonly Label[] _sidebarSteps = new Label[4];
    private readonly Panel[] _stepPanels = new Panel[4];

    private static readonly string[] StepNames = { "Bienvenida", "Servidor central", "Scripts", "Listo" };

    public SetupWizardForm(AppSettings settings)
    {
        _settings = settings;
        _txtScriptsPath.Text = settings.DefaultScriptsPath;

        SuspendLayout();
        ConfigureForm();
        ResumeLayout(false);

        ShowStep(0);
    }

    private void ConfigureForm()
    {
        Text = "Configuración inicial — Alfa Sync Dashboard";
        ClientSize = new Size(700, 476);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(BuildSidebar());

        var host = new Panel { Left = 180, Top = 0, Width = 520, Height = 430 };
        _stepPanels[0] = BuildPanelWelcome();
        _stepPanels[1] = BuildPanelConnection();
        _stepPanels[2] = BuildPanelScripts();
        _stepPanels[3] = BuildPanelSummary();

        foreach (var p in _stepPanels)
        {
            p.Location = Point.Empty;
            p.Size = new Size(520, 430);
            p.Visible = false;
            host.Controls.Add(p);
        }

        Controls.Add(host);
        Controls.Add(BuildNavBar());
    }

    // ── Sidebar ──────────────────────────────────────────────────────────────

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Left = 0, Top = 0, Width = 180, Height = 476,
            BackColor = Color.FromArgb(28, 57, 97)
        };

        sidebar.Controls.Add(new Label
        {
            Text = "Alfa Sync\nDashboard",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Left = 16, Top = 24, Width = 148, Height = 55,
            AutoSize = false
        });

        for (int i = 0; i < StepNames.Length; i++)
        {
            var lbl = new Label
            {
                Text = $"  {i + 1}. {StepNames[i]}",
                ForeColor = Color.FromArgb(140, 175, 220),
                Font = new Font("Segoe UI", 9.5f),
                Left = 0, Top = 108 + i * 44, Width = 180, Height = 36,
                AutoSize = false
            };
            _sidebarSteps[i] = lbl;
            sidebar.Controls.Add(lbl);
        }

        return sidebar;
    }

    // ── Nav bar ───────────────────────────────────────────────────────────────

    private Panel BuildNavBar()
    {
        var nav = new Panel { Left = 180, Top = 430, Width = 520, Height = 46, BackColor = Color.WhiteSmoke };
        nav.Controls.Add(new Label { Left = 0, Top = 0, Width = 520, Height = 1, BackColor = Color.LightGray });

        _btnBack.Left = 280; _btnBack.Top = 8;
        _btnNext.Left = 398; _btnNext.Top = 8;

        _btnBack.Click += (_, _) => PrevStep();
        _btnNext.Click += async (_, _) => await NextStepAsync();

        nav.Controls.Add(_btnBack);
        nav.Controls.Add(_btnNext);
        return nav;
    }

    // ── Step panels ───────────────────────────────────────────────────────────

    private static Panel BuildPanelWelcome()
    {
        var p = new Panel();
        AddTitle(p, "Bienvenido");
        p.Controls.Add(new Label
        {
            Text =
                "Este asistente lo guiará para configurar Alfa Sync Dashboard\n" +
                "por primera vez.\n\n" +
                "Necesitará los siguientes datos:\n\n" +
                "   •  Servidor SQL central (nombre o dirección IP)\n" +
                "   •  Nombre de la base de datos\n" +
                "   •  Usuario y contraseña de SQL Server\n" +
                "   •  Carpeta con los archivos .SQL de sincronización\n\n" +
                "Presione Siguiente para comenzar.",
            Left = 24, Top = 70, Width = 470, Height = 300,
            AutoSize = false, Font = new Font("Segoe UI", 10)
        });
        return p;
    }

    private Panel BuildPanelConnection()
    {
        var p = new Panel();
        AddTitle(p, "Conexión al servidor central");

        // Reset verified flag when any field changes
        void ResetVerified(object? s, EventArgs e)
        {
            _connectionVerified = false;
            _lblConnStatus.Text = string.Empty;
        }
        _txtServer.TextChanged += ResetVerified;
        _txtDatabase.TextChanged += ResetVerified;
        _txtUser.TextChanged += ResetVerified;
        _txtPassword.TextChanged += ResetVerified;

        int y = 68;
        void Row(string label, Control ctrl)
        {
            p.Controls.Add(new Label { Text = label, Left = 24, Top = y + 2, Width = 130, Height = 22, AutoSize = false });
            ctrl.Left = 160; ctrl.Top = y; ctrl.Width = 320; ctrl.Height = 24;
            p.Controls.Add(ctrl);
            y += 34;
        }

        Row("Servidor:", _txtServer);
        Row("Base de datos:", _txtDatabase);
        Row("Usuario:", _txtUser);
        Row("Contraseña:", _txtPassword);

        y += 4;
        _chkTrustCert.Left = 160; _chkTrustCert.Top = y; _chkTrustCert.Width = 300; _chkTrustCert.Height = 22;
        p.Controls.Add(_chkTrustCert);
        y += 26;
        _chkEncrypt.Left = 160; _chkEncrypt.Top = y; _chkEncrypt.Width = 200; _chkEncrypt.Height = 22;
        p.Controls.Add(_chkEncrypt);
        y += 36;

        var btnTest = new Button { Text = "Probar conexión", Left = 160, Top = y, Width = 150, Height = 28 };
        btnTest.Click += async (_, _) => await TestConnectionAsync();
        p.Controls.Add(btnTest);

        _lblConnStatus.Left = 24; _lblConnStatus.Top = y + 36; _lblConnStatus.Width = 460; _lblConnStatus.Height = 24;
        _lblConnStatus.Font = new Font("Segoe UI", 9.5f);
        p.Controls.Add(_lblConnStatus);

        return p;
    }

    private Panel BuildPanelScripts()
    {
        var p = new Panel();
        AddTitle(p, "Carpeta de scripts");

        p.Controls.Add(new Label
        {
            Text = "Indique la carpeta donde se encuentran los archivos .SQL de sincronización.",
            Left = 24, Top = 66, Width = 470, Height = 36,
            AutoSize = false, Font = new Font("Segoe UI", 10)
        });

        _txtScriptsPath.Left = 24; _txtScriptsPath.Top = 112; _txtScriptsPath.Width = 366; _txtScriptsPath.Height = 24;
        _txtScriptsPath.TextChanged += (_, _) => ValidateScriptsPath();
        p.Controls.Add(_txtScriptsPath);

        var btnBrowse = new Button { Text = "Examinar...", Left = 398, Top = 110, Width = 90, Height = 28 };
        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog { SelectedPath = _txtScriptsPath.Text };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                _txtScriptsPath.Text = dlg.SelectedPath;
        };
        p.Controls.Add(btnBrowse);

        _lblScriptsStatus.Left = 24; _lblScriptsStatus.Top = 148; _lblScriptsStatus.Width = 460; _lblScriptsStatus.Height = 24;
        _lblScriptsStatus.Font = new Font("Segoe UI", 9.5f);
        p.Controls.Add(_lblScriptsStatus);

        return p;
    }

    private Panel BuildPanelSummary()
    {
        var p = new Panel();
        AddTitle(p, "Listo para comenzar");

        _lblSummary.Left = 24; _lblSummary.Top = 70; _lblSummary.Width = 470; _lblSummary.Height = 300;
        _lblSummary.Font = new Font("Segoe UI", 10);
        p.Controls.Add(_lblSummary);

        return p;
    }

    // ── Navigation logic ─────────────────────────────────────────────────────

    private void ShowStep(int step)
    {
        _currentStep = step;

        for (int i = 0; i < _stepPanels.Length; i++)
            _stepPanels[i].Visible = i == step;

        for (int i = 0; i < _sidebarSteps.Length; i++)
        {
            bool active = i == step;
            bool done = i < step;
            _sidebarSteps[i].ForeColor = active ? Color.White
                : done ? Color.FromArgb(90, 200, 120)
                : Color.FromArgb(140, 175, 220);
            _sidebarSteps[i].Font = new Font("Segoe UI", 9.5f,
                active ? FontStyle.Bold : FontStyle.Regular);
        }

        _btnBack.Visible = step > 0;
        _btnNext.Text = step == _stepPanels.Length - 1 ? "Finalizar" : "Siguiente >";

        if (step == 2) ValidateScriptsPath();
        if (step == 3) UpdateSummary();
    }

    private async Task NextStepAsync()
    {
        if (_currentStep == 1 && !_connectionVerified)
        {
            MessageBox.Show(
                "Pruebe la conexión antes de continuar.",
                "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_currentStep == _stepPanels.Length - 1)
        {
            ApplySettings();
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        ShowStep(_currentStep + 1);
    }

    private void PrevStep() => ShowStep(_currentStep - 1);

    // ── Actions ───────────────────────────────────────────────────────────────

    private async Task TestConnectionAsync()
    {
        _connectionVerified = false;
        _lblConnStatus.ForeColor = Color.DimGray;
        _lblConnStatus.Text = "Probando conexión...";

        try
        {
            await using var cn = new SqlConnection(BuildConnectionString());
            await cn.OpenAsync();
            _lblConnStatus.Text = "✔  Conexión exitosa";
            _lblConnStatus.ForeColor = Color.FromArgb(0, 140, 0);
            _connectionVerified = true;
        }
        catch (Exception ex)
        {
            _lblConnStatus.Text = $"✘  {ex.Message}";
            _lblConnStatus.ForeColor = Color.DarkRed;
        }
    }

    private void ValidateScriptsPath()
    {
        var path = _txtScriptsPath.Text.Trim();
        if (string.IsNullOrEmpty(path)) { _lblScriptsStatus.Text = string.Empty; return; }

        if (!Directory.Exists(path))
        {
            _lblScriptsStatus.Text = "✘  La carpeta no existe";
            _lblScriptsStatus.ForeColor = Color.DarkRed;
            return;
        }

        var count = Directory.GetFiles(path, "*.SQL", SearchOption.TopDirectoryOnly).Length
                  + Directory.GetFiles(path, "*.sql", SearchOption.TopDirectoryOnly).Length;

        if (count == 0)
        {
            _lblScriptsStatus.Text = "⚠  No se encontraron archivos .SQL en la carpeta";
            _lblScriptsStatus.ForeColor = Color.FromArgb(180, 100, 0);
        }
        else
        {
            _lblScriptsStatus.Text = $"✔  {count} archivo{(count != 1 ? "s" : "")} .SQL encontrado{(count != 1 ? "s" : "")}";
            _lblScriptsStatus.ForeColor = Color.FromArgb(0, 140, 0);
        }
    }

    private void UpdateSummary() =>
        _lblSummary.Text =
            "La configuración fue completada correctamente.\n\n" +
            $"Servidor:        {_txtServer.Text.Trim()}\n" +
            $"Base de datos:   {_txtDatabase.Text.Trim()}\n" +
            $"Usuario:         {_txtUser.Text.Trim()}\n" +
            $"Scripts:         {_txtScriptsPath.Text.Trim()}\n\n" +
            "Haga clic en Finalizar para guardar y abrir la aplicación.";

    private void ApplySettings()
    {
        _settings.CentralConnectionString = BuildConnectionString();
        _settings.DefaultScriptsPath = _txtScriptsPath.Text.Trim();
    }

    private string BuildConnectionString() =>
        $"Server={_txtServer.Text.Trim()};" +
        $"Database={_txtDatabase.Text.Trim()};" +
        $"User Id={_txtUser.Text.Trim()};" +
        $"Password={_txtPassword.Text};" +
        $"TrustServerCertificate={(_chkTrustCert.Checked ? "True" : "False")};" +
        $"Encrypt={(_chkEncrypt.Checked ? "True" : "False")};";

    private static void AddTitle(Panel p, string text) =>
        p.Controls.Add(new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 57, 97),
            Left = 24, Top = 20, Width = 470, Height = 34,
            AutoSize = false
        });
}
