using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class RegisterForm : XtraForm
{
    public RegisterForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Yeni Hesap Oluştur";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(400, 400);

        var panelMain = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(30) };

        var lblTitle = new LabelControl
        {
            Text = "Yeni Hesap Oluştur",
            Location = new Point(100, 20),
            AutoSizeMode = LabelAutoSizeMode.None,
            Size = new Size(200, 30),
            Appearance = { Font = new Font("Segoe UI", 14, FontStyle.Bold), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
        };

        var lblFullName = new LabelControl { Text = "Ad Soyad", Location = new Point(50, 60) };
        var txtFullName = new TextEdit { Location = new Point(50, 80), Size = new Size(280, 28) };

        var lblEmail = new LabelControl { Text = "E-posta", Location = new Point(50, 115) };
        var txtEmail = new TextEdit { Location = new Point(50, 135), Size = new Size(280, 28) };

        var lblUsername = new LabelControl { Text = "Kullanıcı Adı", Location = new Point(50, 170) };
        var txtUsername = new TextEdit { Location = new Point(50, 190), Size = new Size(280, 28) };

        var lblPassword = new LabelControl { Text = "Şifre", Location = new Point(50, 225) };
        var txtPassword = new TextEdit { Location = new Point(50, 245), Size = new Size(280, 28) };
        txtPassword.Properties.PasswordChar = '●';

        var lblConfirm = new LabelControl { Text = "Şifre Tekrar", Location = new Point(50, 280) };
        var txtConfirm = new TextEdit { Location = new Point(50, 300), Size = new Size(280, 28) };
        txtConfirm.Properties.PasswordChar = '●';

        var btnRegister = new SimpleButton
        {
            Text = "Kayıt Ol",
            Location = new Point(50, 345),
            Size = new Size(135, 36),
            Appearance = { BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White }
        };
        btnRegister.Click += async (s, e) => await RegisterAsync(txtFullName.Text, txtEmail.Text, txtUsername.Text, txtPassword.Text, txtConfirm.Text);

        var btnCancel = new SimpleButton
        {
            Text = "İptal",
            Location = new Point(195, 345),
            Size = new Size(135, 36)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panelMain.Controls.AddRange(new Control[] { lblTitle, lblFullName, txtFullName, lblEmail, txtEmail, lblUsername, txtUsername, lblPassword, txtPassword, lblConfirm, txtConfirm, btnRegister, btnCancel });
        Controls.Add(panelMain);
    }

    private async Task RegisterAsync(string fullName, string email, string username, string password, string confirm)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            XtraMessageBox.Show("Kullanıcı adı ve şifre zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (password != confirm)
        {
            XtraMessageBox.Show("Şifreler eşleşmiyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var userService = new UserService(unitOfWork);

            await userService.RegisterAsync(username, password, email, fullName);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

