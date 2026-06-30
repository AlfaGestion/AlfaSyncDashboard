using AlfaSyncDashboard.Models;
using Microsoft.Data.SqlClient;

namespace AlfaSyncDashboard.Forms;

public sealed class SettingsForm : Form
{
    private readonly TextBox _txtServer = new();
    private readonly TextBox _txtDatabase = new();
    private readonly TextBox _txtUser = new();
    private readonly TextBox _txtPassword = new() { UseSystemPasswordChar = true };
    private readonly CheckBox _chkTrustCert = new() { Text = "Confiar en certificado del servidor", Checked = true };
    private readonly CheckBox _chkEncrypt = new() { Text = "Cifrar conexión", Checked = false };
    private readonly Label _lblConnStatus = new() { AutoSize = false };

    public AppSettings Settings { get; }

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;
        SuspendLayout();
        BuildForm();
        LoadValues(settings);
        ResumeLayout(false);
    }

    private void BuildForm()
    {
        Text = "Configuración";
        ClientSize = new Size(540, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        int y = 14;

        Controls.Add(new Label
        {
            Text = "Conexión central",
            Font = new Font(Font, FontStyle.Bold),
            Left = 14, Top = y, Width = 300, Height = 20, AutoSize = false
        });
        y += 22;
        Controls.Add(new Label { Left = 14, Top = y, Width = 512, Height = 1, BackColor = Color.LightGray });
        y += 10;

        void Row(string label, Control ctrl)
        {
            AddLabel(label, 14, y + 2);
            ctrl.Left = 170; ctrl.Top = y; ctrl.Width = 356; ctrl.Height = 24;
            Controls.Add(ctrl);
            y += 32;
        }

        Row("Servidor:", _txtServer);
        Row("Base de datos:", _txtDatabase);
        Row("Usuario:", _txtUser);
        Row("Contraseña:", _txtPassword);

        y += 2;
        _chkTrustCert.Left = 170; _chkTrustCert.Top = y; _chkTrustCert.Width = 340; _chkTrustCert.Height = 22;
        Controls.Add(_chkTrustCert);
        y += 26;
        _chkEncrypt.Left = 170; _chkEncrypt.Top = y; _chkEncrypt.Width = 220; _chkEncrypt.Height = 22;
        Controls.Add(_chkEncrypt);
        y += 34;

        var btnTest = new Button { Text = "Probar conexión", Left = 170, Top = y, Width = 150, Height = 28 };
        btnTest.Click += async (_, _) => await TestConnectionAsync();
        Controls.Add(btnTest);

        _lblConnStatus.Left = 14; _lblConnStatus.Top = y + 34; _lblConnStatus.Width = 512; _lblConnStatus.Height = 22;
        _lblConnStatus.Font = new Font(Font.FontFamily, 9f);
        Controls.Add(_lblConnStatus);
        y += 62;

        var btnSave = new Button { Text = "Guardar", Left = 316, Top = y, Width = 100, Height = 30 };
        btnSave.Click += (_, _) => SaveAndClose();

        var btnCancel = new Button { Text = "Cancelar", Left = 426, Top = y, Width = 100, Height = 30 };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private void LoadValues(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.CentralConnectionString))
            return;

        try
        {
            var b = new SqlConnectionStringBuilder(settings.CentralConnectionString);
            _txtServer.Text = b.DataSource;
            _txtDatabase.Text = b.InitialCatalog;
            _txtUser.Text = b.UserID;
            _txtPassword.Text = b.Password;
            _chkEncrypt.Checked = b.Encrypt == SqlConnectionEncryptOption.Mandatory;
            _chkTrustCert.Checked = b.TrustServerCertificate;
        }
        catch
        {
        }
    }

    private async Task TestConnectionAsync()
    {
        _lblConnStatus.ForeColor = Color.DimGray;
        _lblConnStatus.Text = "Probando conexión...";

        try
        {
            await using var cn = new SqlConnection(BuildConnectionString());
            await cn.OpenAsync();
            _lblConnStatus.Text = "✔  Conexión exitosa";
            _lblConnStatus.ForeColor = Color.FromArgb(0, 140, 0);
        }
        catch (Exception ex)
        {
            _lblConnStatus.Text = $"✘  {ex.Message}";
            _lblConnStatus.ForeColor = Color.DarkRed;
        }
    }

    private void SaveAndClose()
    {
        Settings.CentralConnectionString = BuildConnectionString();
        DialogResult = DialogResult.OK;
        Close();
    }

    private string BuildConnectionString() =>
        $"Server={_txtServer.Text.Trim()};" +
        $"Database={_txtDatabase.Text.Trim()};" +
        $"User Id={_txtUser.Text.Trim()};" +
        $"Password={_txtPassword.Text};" +
        $"TrustServerCertificate={(_chkTrustCert.Checked ? "True" : "False")};" +
        $"Encrypt={(_chkEncrypt.Checked ? "True" : "False")};";

    private void AddLabel(string text, int x, int y) =>
        Controls.Add(new Label { Text = text, Left = x, Top = y, Width = 152, Height = 20, AutoSize = false });
}
