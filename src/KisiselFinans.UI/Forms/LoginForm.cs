using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class LoginForm : Form
{
    public User? CurrentUser { get; private set; }

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private Panel cardPanel = null!;

    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Kişisel Finans";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.PrimaryDark;
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        // Sol Panel - Premium Gradient
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 520
        };
        leftPanel.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, leftPanel.Width, leftPanel.Height);
            AppTheme.DrawGradientBackground(e.Graphics, rect);
        };

        // Sol Panel İçerik
        var leftContent = new Panel
        {
            Size = new Size(420, 450),
            BackColor = Color.Transparent
        };
        leftPanel.Resize += (s, e) =>
        {
            leftContent.Location = new Point(
                (leftPanel.Width - leftContent.Width) / 2,
                (leftPanel.Height - leftContent.Height) / 2);
        };

        var lblIcon = new Label
        {
            Text = "💎",
            Font = new Font("Segoe UI Emoji", 80),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 130
        };

        var lblBrand = new Label
        {
            Text = "Kişisel Finans",
            Font = new Font("Segoe UI Light", 36),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 60
        };

        var lblSlogan = new Label
        {
            Text = "Akıllı Finansal Yönetim",
            Font = new Font("Segoe UI", 15),
            ForeColor = Color.FromArgb(220, 255, 255, 255),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 35
        };

        var divider = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.Transparent
        };

        var features = new (string icon, string text)[]
        {
            ("📊", "Gelir ve gider analizi"),
            ("🎯", "Akıllı bütçe hedefleri"),
            ("📈", "Gerçek zamanlı raporlar"),
            ("🔮", "Finansal tahminleme"),
            ("🔒", "Güvenli veri saklama")
        };

        var featuresPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 180,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(60, 10, 60, 0),
            BackColor = Color.Transparent
        };

        foreach (var (icon, text) in features)
        {
            var featureLabel = new Label
            {
                Text = $"{icon}  {text}",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(240, 255, 255, 255),
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6)
            };
            featuresPanel.Controls.Add(featureLabel);
        }

        leftContent.Controls.Add(featuresPanel);
        leftContent.Controls.Add(divider);
        leftContent.Controls.Add(lblSlogan);
        leftContent.Controls.Add(lblBrand);
        leftContent.Controls.Add(lblIcon);
        leftPanel.Controls.Add(leftContent);

        // Sağ Panel
        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.PrimaryDark
        };

        // Kapat Butonu
        var btnClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 18),
            ForeColor = AppTheme.TextMuted,
            Size = new Size(50, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnClose.Click += (s, e) => Close();
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = AppTheme.AccentRed;
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = AppTheme.TextMuted;

        // Login Card
        cardPanel = new Panel
        {
            Size = new Size(420, 500),
            BackColor = AppTheme.CardBg
        };

        rightPanel.Resize += (s, e) =>
        {
            cardPanel.Location = new Point(
                (rightPanel.Width - cardPanel.Width) / 2,
                (rightPanel.Height - cardPanel.Height) / 2);
            btnClose.Location = new Point(rightPanel.Width - 70, 20);
        };

        // Decorative top line
        var topLine = new Panel
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = AppTheme.GradientStart
        };
        topLine.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, topLine.Width, topLine.Height);
            AppTheme.DrawGradientBackground(e.Graphics, rect, false);
        };

        // Card İçeriği
        var lblTitle = new Label
        {
            Text = "Hoş Geldiniz",
            Font = new Font("Segoe UI Light", 28),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(45, 50),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Hesabınıza giriş yapın",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(45, 95),
            AutoSize = true
        };

        // Kullanıcı Adı
        var lblUsername = new Label
        {
            Text = "KULLANICI ADI",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = AppTheme.TextMuted,
            Location = new Point(45, 150),
            AutoSize = true
        };

        txtUsername = new TextBox
        {
            Location = new Point(45, 175),
            Size = new Size(330, 45),
            Font = new Font("Segoe UI", 13),
            BackColor = AppTheme.InputBg,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Şifre
        var lblPassword = new Label
        {
            Text = "ŞİFRE",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = AppTheme.TextMuted,
            Location = new Point(45, 235),
            AutoSize = true
        };

        txtPassword = new TextBox
        {
            Location = new Point(45, 260),
            Size = new Size(330, 45),
            Font = new Font("Segoe UI", 13),
            BackColor = AppTheme.InputBg,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true
        };

        // Giriş Butonu
        var btnLogin = new Button
        {
            Text = "GİRİŞ YAP  →",
            Location = new Point(45, 340),
            Size = new Size(330, 55),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.GradientStart,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 13),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += async (s, e) => await LoginAsync();
        btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = AppTheme.GradientMid;
        btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = AppTheme.GradientStart;

        // Kayıt Linki
        var lblNoAccount = new Label
        {
            Text = "Hesabınız yok mu?",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(95, 420),
            AutoSize = true
        };

        var btnRegister = new Label
        {
            Text = "Kayıt Olun",
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = AppTheme.AccentCyan,
            Location = new Point(220, 420),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        btnRegister.Click += (s, e) => ShowRegisterDialog();
        btnRegister.MouseEnter += (s, e) => btnRegister.ForeColor = AppTheme.AccentPink;
        btnRegister.MouseLeave += (s, e) => btnRegister.ForeColor = AppTheme.AccentCyan;

        txtPassword.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) await LoginAsync();
        };

        cardPanel.Controls.AddRange(new Control[]
        {
            topLine, lblTitle, lblSubtitle,
            lblUsername, txtUsername,
            lblPassword, txtPassword,
            btnLogin, lblNoAccount, btnRegister
        });

        rightPanel.Controls.Add(cardPanel);
        rightPanel.Controls.Add(btnClose);

        Controls.Add(rightPanel);
        Controls.Add(leftPanel);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            ShowError("Kullanıcı adı ve şifre boş olamaz.");
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var userService = new UserService(unitOfWork);

            CurrentUser = await userService.AuthenticateAsync(txtUsername.Text, txtPassword.Text);

            if (CurrentUser == null)
            {
                ShowError("Kullanıcı adı veya şifre hatalı.");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Bağlantı hatası: {ex.Message}");
        }
    }

    private void ShowRegisterDialog()
    {
        using var registerForm = new RegisterForm();
        if (registerForm.ShowDialog() == DialogResult.OK)
        {
            txtUsername.Text = registerForm.RegisteredUsername;
            MessageBox.Show("Kayıt başarılı! Şimdi giriş yapabilirsiniz.", "✓ Başarılı",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
