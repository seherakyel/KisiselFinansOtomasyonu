using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class LoginForm : XtraForm
{
    public User? CurrentUser { get; private set; }

    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Kişisel Finans - Giriş";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Size = new Size(400, 350);
        this.BackColor = Color.FromArgb(240, 244, 248);

        // Panel
        var panelMain = new PanelControl
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30)
        };

        // Logo/Başlık
        var lblTitle = new LabelControl
        {
            Text = "💰 Kişisel Finans",
            Location = new Point(100, 30),
            AutoSizeMode = LabelAutoSizeMode.None,
            Size = new Size(200, 40),
            Appearance = { Font = new Font("Segoe UI", 18, FontStyle.Bold), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
        };

        var lblSubtitle = new LabelControl
        {
            Text = "Hesabınıza giriş yapın",
            Location = new Point(100, 70),
            AutoSizeMode = LabelAutoSizeMode.None,
            Size = new Size(200, 20),
            Appearance = { TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center }, ForeColor = Color.Gray }
        };

        // Kullanıcı Adı
        var lblUsername = new LabelControl { Text = "Kullanıcı Adı", Location = new Point(50, 110) };
        var txtUsername = new TextEdit { Location = new Point(50, 130), Size = new Size(280, 28) };
        txtUsername.Properties.NullValuePrompt = "Kullanıcı adınızı girin";

        // Şifre
        var lblPassword = new LabelControl { Text = "Şifre", Location = new Point(50, 170) };
        var txtPassword = new TextEdit { Location = new Point(50, 190), Size = new Size(280, 28) };
        txtPassword.Properties.PasswordChar = '●';
        txtPassword.Properties.NullValuePrompt = "Şifrenizi girin";

        // Giriş Butonu
        var btnLogin = new SimpleButton
        {
            Text = "Giriş Yap",
            Location = new Point(50, 240),
            Size = new Size(280, 36),
            Appearance = { BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) }
        };
        btnLogin.Click += async (s, e) => await LoginAsync(txtUsername.Text, txtPassword.Text);

        // Kayıt Linki
        var btnRegister = new HyperlinkLabelControl
        {
            Text = "Hesabınız yok mu? Kayıt olun",
            Location = new Point(100, 285),
            AutoSizeMode = LabelAutoSizeMode.None,
            Size = new Size(200, 20)
        };
        btnRegister.Click += (s, e) => ShowRegisterDialog();

        // Enter tuşu ile giriş
        txtPassword.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                await LoginAsync(txtUsername.Text, txtPassword.Text);
        };

        panelMain.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnRegister });
        Controls.Add(panelMain);
    }

    private async Task LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            XtraMessageBox.Show("Kullanıcı adı ve şifre boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var userService = new UserService(unitOfWork);

            CurrentUser = await userService.AuthenticateAsync(username, password);

            if (CurrentUser == null)
            {
                XtraMessageBox.Show("Kullanıcı adı veya şifre hatalı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Giriş hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowRegisterDialog()
    {
        using var registerForm = new RegisterForm();
        if (registerForm.ShowDialog() == DialogResult.OK)
        {
            XtraMessageBox.Show("Kayıt başarılı! Şimdi giriş yapabilirsiniz.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

