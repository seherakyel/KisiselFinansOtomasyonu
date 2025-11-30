using KisiselFinans.Business.Services;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class RegisterForm : Form
{
    public string RegisteredUsername { get; private set; } = string.Empty;

    private TextBox txtFullName = null!;
    private TextBox txtEmail = null!;
    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private TextBox txtConfirm = null!;
    private Panel cardPanel = null!;

    public RegisterForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Kayıt Ol";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.PrimaryDark;
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        // Sol Panel - Farklı Gradient
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 520
        };
        leftPanel.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, leftPanel.Width, leftPanel.Height);
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, AppTheme.AccentGreen, AppTheme.AccentCyan,
                System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRectangle(brush, rect);
        };

        // Sol Panel İçerik
        var leftContent = new Panel
        {
            Size = new Size(420, 380),
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
            Text = "🚀",
            Font = new Font("Segoe UI Emoji", 80),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 130
        };

        var lblBrand = new Label
        {
            Text = "Yeni Başlangıç",
            Font = new Font("Segoe UI Light", 34),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 55
        };

        var lblSlogan = new Label
        {
            Text = "Finansal özgürlüğe ilk adım",
            Font = new Font("Segoe UI", 14),
            ForeColor = Color.FromArgb(230, 255, 255, 255),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 35
        };

        var benefitsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 140,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(80, 25, 80, 0),
            BackColor = Color.Transparent
        };

        var benefits = new[] { "✓ Tamamen ücretsiz", "✓ Sınırsız hesap", "✓ Anlık senkronizasyon" };
        foreach (var benefit in benefits)
        {
            benefitsPanel.Controls.Add(new Label
            {
                Text = benefit,
                Font = new Font("Segoe UI Semibold", 13),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 8)
            });
        }

        leftContent.Controls.Add(benefitsPanel);
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
        btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = AppTheme.AccentRed;
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = AppTheme.TextMuted;

        // Register Card
        cardPanel = new Panel
        {
            Size = new Size(440, 620),
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
            BackColor = AppTheme.AccentGreen
        };
        topLine.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, topLine.Width, topLine.Height);
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, AppTheme.AccentGreen, AppTheme.AccentCyan,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, rect);
        };

        // Card İçeriği
        var lblTitle = new Label
        {
            Text = "Hesap Oluştur",
            Font = new Font("Segoe UI Light", 26),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(45, 40),
            AutoSize = true
        };

        int y = 95;
        const int fieldSpacing = 80;

        // Ad Soyad
        var lblFullName = CreateFieldLabel("AD SOYAD", y);
        txtFullName = CreateTextField(y + 22);

        y += fieldSpacing;
        var lblEmail = CreateFieldLabel("E-POSTA", y);
        txtEmail = CreateTextField(y + 22);

        y += fieldSpacing;
        var lblUsername = CreateFieldLabel("KULLANICI ADI", y);
        txtUsername = CreateTextField(y + 22);

        y += fieldSpacing;
        var lblPassword = CreateFieldLabel("ŞİFRE", y);
        txtPassword = CreateTextField(y + 22, true);

        y += fieldSpacing;
        var lblConfirm = CreateFieldLabel("ŞİFRE TEKRAR", y);
        txtConfirm = CreateTextField(y + 22, true);

        y += fieldSpacing + 5;

        // Butonlar
        var btnRegister = new Button
        {
            Text = "KAYIT OL  ✓",
            Location = new Point(45, y),
            Size = new Size(170, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentGreen,
            ForeColor = AppTheme.PrimaryDark,
            Font = new Font("Segoe UI Semibold", 12),
            Cursor = Cursors.Hand
        };
        btnRegister.FlatAppearance.BorderSize = 0;
        btnRegister.Click += async (s, e) => await RegisterAsync();
        btnRegister.MouseEnter += (s, e) => btnRegister.BackColor = AppTheme.AccentCyan;
        btnRegister.MouseLeave += (s, e) => btnRegister.BackColor = AppTheme.AccentGreen;

        var btnCancel = new Button
        {
            Text = "← GERİ",
            Location = new Point(225, y),
            Size = new Size(170, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font("Segoe UI Semibold", 12),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        cardPanel.Controls.AddRange(new Control[]
        {
            topLine, lblTitle,
            lblFullName, txtFullName,
            lblEmail, txtEmail,
            lblUsername, txtUsername,
            lblPassword, txtPassword,
            lblConfirm, txtConfirm,
            btnRegister, btnCancel
        });

        rightPanel.Controls.Add(cardPanel);
        rightPanel.Controls.Add(btnClose);

        Controls.Add(rightPanel);
        Controls.Add(leftPanel);
    }

    private Label CreateFieldLabel(string text, int y) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = AppTheme.TextMuted,
        Location = new Point(45, y),
        AutoSize = true
    };

    private TextBox CreateTextField(int y, bool isPassword = false)
    {
        var txt = new TextBox
        {
            Location = new Point(45, y),
            Size = new Size(350, 42),
            Font = new Font("Segoe UI", 12),
            BackColor = AppTheme.InputBg,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = isPassword
        };
        return txt;
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            MessageBox.Show("Kullanıcı adı ve şifre zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (txtPassword.Text != txtConfirm.Text)
        {
            MessageBox.Show("Şifreler eşleşmiyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (txtPassword.Text.Length < 4)
        {
            MessageBox.Show("Şifre en az 4 karakter olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var userService = new UserService(unitOfWork);

            await userService.RegisterAsync(txtUsername.Text, txtPassword.Text, txtEmail.Text, txtFullName.Text);
            RegisteredUsername = txtUsername.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
