using KisiselFinans.Core.Entities;
using KisiselFinans.UI.Controls;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class MainForm : Form
{
    private readonly User _currentUser;
    private Panel _contentPanel = null!;
    private Panel _sidebarPanel = null!;
    private UserControl? _currentControl;
    private readonly Dictionary<string, Button> _menuButtons = new();
    private Button? _activeButton;

    public MainForm(User currentUser)
    {
        _currentUser = currentUser;
        InitializeComponent();
        LoadDashboard();
    }

    private void InitializeComponent()
    {
        Text = "Kisisel Finans Otomasyonu";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.PrimaryDark;
        DoubleBuffered = true;

        // Header
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = AppTheme.CardBg
        };

        var headerLine = new Panel { Dock = DockStyle.Bottom, Height = 2 };
        headerLine.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, headerLine.Width, headerLine.Height);
            AppTheme.DrawGradientBackground(e.Graphics, rect, false);
        };

        var lblAppName = new Label
        {
            Text = "Kisisel Finans",
            Font = new Font("Segoe UI Light", 20),
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(280, 18)
        };

        var userPanel = new Panel
        {
            Size = new Size(220, 40),
            BackColor = AppTheme.PrimaryLight,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };

        var lblUser = new Label
        {
            Text = _currentUser.FullName ?? _currentUser.Username,
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        userPanel.Controls.Add(lblUser);

        var btnMinimize = CreateWindowButton("-");
        btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

        var btnMaximize = CreateWindowButton("O");
        btnMaximize.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal : FormWindowState.Maximized;

        var btnClose = CreateWindowButton("X");
        btnClose.Click += (s, e) => Close();
        btnClose.MouseEnter += (s, e) => { btnClose.BackColor = AppTheme.AccentRed; btnClose.ForeColor = Color.White; };
        btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = AppTheme.TextMuted; };

        headerPanel.Resize += (s, e) =>
        {
            userPanel.Location = new Point(headerPanel.Width - 480, 12);
            btnClose.Location = new Point(headerPanel.Width - 50, 0);
            btnMaximize.Location = new Point(headerPanel.Width - 95, 0);
            btnMinimize.Location = new Point(headerPanel.Width - 140, 0);
        };

        headerPanel.Controls.AddRange(new Control[] { headerLine, lblAppName, userPanel, btnMinimize, btnMaximize, btnClose });

        // Sidebar
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 260,
            BackColor = AppTheme.CardBg
        };

        var logoArea = new Panel { Dock = DockStyle.Top, Height = 80 };
        logoArea.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, logoArea.Width, logoArea.Height);
            AppTheme.DrawGradientBackground(e.Graphics, rect);
        };

        var lblLogo = new Label
        {
            Text = "FINANS",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(70, 22),
            AutoSize = true
        };
        logoArea.Controls.Add(lblLogo);

        var menuContainer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(15, 20, 15, 20),
            BackColor = AppTheme.CardBg
        };

        AddMenuGroup(menuContainer, "GENEL");
        CreateMenuButton(menuContainer, "Dashboard", () => LoadDashboard());

        AddMenuGroup(menuContainer, "ISLEMLER");
        CreateMenuButton(menuContainer, "Gelir Ekle", () => ShowTransactionDialog(1));
        CreateMenuButton(menuContainer, "Gider Ekle", () => ShowTransactionDialog(2));
        CreateMenuButton(menuContainer, "Transfer", () => ShowTransferDialog());
        CreateMenuButton(menuContainer, "Tum Islemler", () => LoadControl(new TransactionListControl(_currentUser.Id)));

        AddMenuGroup(menuContainer, "YONETIM");
        CreateMenuButton(menuContainer, "Hesaplar", () => LoadControl(new AccountListControl(_currentUser.Id)));
        CreateMenuButton(menuContainer, "Butceler", () => LoadControl(new BudgetListControl(_currentUser.Id)));
        CreateMenuButton(menuContainer, "Planli Islemler", () => LoadControl(new ScheduledListControl(_currentUser.Id)));
        CreateMenuButton(menuContainer, "Kategoriler", () => LoadControl(new CategoryListControl(_currentUser.Id)));

        AddMenuGroup(menuContainer, "ANALIZ");
        CreateMenuButton(menuContainer, "Raporlar", () => ShowReportDialog());

        _sidebarPanel.Controls.Add(menuContainer);
        _sidebarPanel.Controls.Add(logoArea);

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.PrimaryDark,
            Padding = new Padding(30)
        };

        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = AppTheme.CardBg
        };

        var lblStatus = new Label
        {
            Text = $"  {DateTime.Now:dd MMMM yyyy}",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0)
        };

        var lblVersion = new Label
        {
            Text = "v1.0.0  ",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 20, 0)
        };

        statusPanel.Controls.AddRange(new Control[] { lblStatus, lblVersion });

        Controls.Add(_contentPanel);
        Controls.Add(_sidebarPanel);
        Controls.Add(statusPanel);
        Controls.Add(headerPanel);
    }

    private Label CreateWindowButton(string text)
    {
        var btn = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 11),
            ForeColor = AppTheme.TextMuted,
            Size = new Size(45, 65),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent
        };
        btn.MouseEnter += (s, e) => { if (text != "X") btn.BackColor = AppTheme.Surface; };
        btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
        return btn;
    }

    private void AddMenuGroup(FlowLayoutPanel container, string title)
    {
        var lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = AppTheme.AccentPurple,
            Size = new Size(230, 35),
            Padding = new Padding(12, 18, 0, 5)
        };
        container.Controls.Add(lbl);
    }

    private void CreateMenuButton(FlowLayoutPanel container, string text, Action onClick)
    {
        var btn = new Button
        {
            Text = $"   {text}",
            Size = new Size(230, 48),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11),
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 0, 0, 0),
            Margin = new Padding(0, 3, 0, 3)
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = AppTheme.PrimaryLight;

        btn.Click += (s, e) =>
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = Color.Transparent;
                _activeButton.ForeColor = AppTheme.TextSecondary;
            }
            btn.BackColor = AppTheme.GradientStart;
            btn.ForeColor = AppTheme.TextPrimary;
            _activeButton = btn;
            onClick();
        };

        _menuButtons[text] = btn;
        container.Controls.Add(btn);
    }

    private void LoadControl(UserControl control)
    {
        _currentControl?.Dispose();
        _currentControl = control;
        control.Dock = DockStyle.Fill;
        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(control);
    }

    private void LoadDashboard()
    {
        if (_menuButtons.TryGetValue("Dashboard", out var btn))
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = Color.Transparent;
                _activeButton.ForeColor = AppTheme.TextSecondary;
            }
            btn.BackColor = AppTheme.GradientStart;
            btn.ForeColor = AppTheme.TextPrimary;
            _activeButton = btn;
        }
        LoadControl(new DashboardControl(_currentUser.Id));
    }

    private void ShowTransactionDialog(byte type)
    {
        using var dialog = new TransactionDialog(_currentUser.Id, type);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (_currentControl is DashboardControl dc) dc.RefreshData();
            else if (_currentControl is TransactionListControl tc) tc.RefreshData();
        }
    }

    private void ShowTransferDialog()
    {
        using var dialog = new TransferDialog(_currentUser.Id);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (_currentControl is DashboardControl dc) dc.RefreshData();
        }
    }

    private void ShowReportDialog()
    {
        using var dialog = new ReportForm(_currentUser.Id);
        dialog.ShowDialog();
    }
}
