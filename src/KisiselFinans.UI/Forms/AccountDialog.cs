using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class AccountDialog : Form
{
    private readonly int _userId;
    private readonly int? _accountId;
    private Account? _account;

    private TextBox _txtName = null!;
    private ComboBox _cmbType = null!;
    private ComboBox _cmbCurrency = null!;
    private TextBox _txtInitialBalance = null!;
    private TextBox _txtCreditLimit = null!;
    private NumericUpDown _txtCutoffDay = null!;
    private List<AccountType> _accountTypes = new();

    private static readonly Color AccentColor = Color.FromArgb(168, 85, 247);
    private static readonly Color AccentLight = Color.FromArgb(192, 132, 252);
    private static readonly Color BgDark = Color.FromArgb(17, 24, 39);
    private static readonly Color BgCard = Color.FromArgb(31, 41, 55);
    private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public AccountDialog(int userId, int? accountId)
    {
        _userId = userId;
        _accountId = accountId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _accountId.HasValue;
        Text = isEdit ? "Hesap Duzenle" : "Yeni Hesap";
        Size = new Size(480, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = BgDark;

        var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };

        // ===== HEADER =====
        var header = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = BgDark };

        var iconPanel = new Panel { Size = new Size(56, 56), Location = new Point(35, 17) };
        iconPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(AccentColor);
            e.Graphics.FillEllipse(brush, 0, 0, 55, 55);
            using var font = new Font("Segoe UI", 22);
            e.Graphics.DrawString("🏦", font, Brushes.White, 8, 8);
        };

        header.Controls.Add(iconPanel);
        header.Controls.Add(new Label
        {
            Text = isEdit ? "Hesap Duzenle" : "Yeni Hesap",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(105, 22),
            AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = "Hesap bilgilerini girin",
            Font = new Font("Segoe UI", 11),
            ForeColor = TextMuted,
            Location = new Point(107, 52),
            AutoSize = true
        });
        header.Controls.Add(CreateCloseButton());

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderColor };

        // ===== CONTENT =====
        var content = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(35, 20, 35, 20) };

        int y = 0;

        // HESAP ADI
        content.Controls.Add(CreateLabel("Hesap Adi", y));
        _txtName = CreateTextBox(y + 28);
        content.Controls.Add(_txtName);

        y += 75;

        // HESAP TURU & PARA BIRIMI
        content.Controls.Add(CreateLabel("Hesap Turu", y));
        _cmbType = CreateComboBox(y + 28, 190);
        content.Controls.Add(_cmbType);

        content.Controls.Add(CreateLabel("Para Birimi", y, 205));
        _cmbCurrency = CreateComboBox(y + 28, 185, 205);
        _cmbCurrency.Items.AddRange(new[] { "TRY - Turk Lirasi", "USD - Dolar", "EUR - Euro", "GBP - Sterlin" });
        _cmbCurrency.SelectedIndex = 0;
        content.Controls.Add(_cmbCurrency);

        y += 75;

        // BASLANGIC BAKIYESI & KREDI LIMITI
        content.Controls.Add(CreateLabel("Baslangic Bakiyesi", y));
        _txtInitialBalance = CreateAmountTextBox(y + 28, 190);
        content.Controls.Add(_txtInitialBalance);

        content.Controls.Add(CreateLabel("Kredi Limiti", y, 205));
        _txtCreditLimit = CreateAmountTextBox(y + 28, 185, 205);
        content.Controls.Add(_txtCreditLimit);

        y += 75;

        // KESIM GUNU
        content.Controls.Add(CreateLabel("Hesap Kesim Gunu (Kredi Karti icin)", y));
        _txtCutoffDay = new NumericUpDown
        {
            Location = new Point(0, y + 28),
            Size = new Size(120, 40),
            Minimum = 0,
            Maximum = 31,
            Font = new Font("Segoe UI", 12),
            BackColor = BgCard,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(_txtCutoffDay);

        // ===== FOOTER =====
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 85,
            BackColor = Color.FromArgb(24, 32, 48)
        };

        var btnCancel = CreateButton("Vazgec", BorderColor, 155, 120);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = CreateButton("Kaydet", Color.FromArgb(34, 197, 94), 285, 140);
        btnSave.Click += async (s, e) => await SaveAsync();

        footer.Controls.AddRange(new Control[] { btnCancel, btnSave });

        var accentLine = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = AccentColor };

        mainPanel.Controls.Add(content);
        mainPanel.Controls.Add(divider);
        mainPanel.Controls.Add(header);
        mainPanel.Controls.Add(footer);
        mainPanel.Controls.Add(accentLine);

        Controls.Add(mainPanel);
    }

    private Label CreateCloseButton()
    {
        var btn = new Label
        {
            Text = "✕", Font = new Font("Segoe UI", 14),
            ForeColor = Color.FromArgb(100, 116, 139),
            Size = new Size(44, 44), Location = new Point(420, 10),
            TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btn.MouseEnter += (s, e) => btn.ForeColor = Color.FromArgb(239, 68, 68);
        btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(100, 116, 139);
        return btn;
    }

    private Label CreateLabel(string text, int y, int x = 0) => new()
    {
        Text = text, Font = new Font("Segoe UI Semibold", 10),
        ForeColor = Color.White, Location = new Point(x, y), AutoSize = true
    };

    private TextBox CreateTextBox(int y, int width = 390, int x = 0) => new()
    {
        Location = new Point(x, y), Size = new Size(width, 40),
        Font = new Font("Segoe UI", 12), BackColor = BgCard,
        ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
    };

    private TextBox CreateAmountTextBox(int y, int width, int x = 0)
    {
        var txt = new TextBox
        {
            Location = new Point(x, y), Size = new Size(width, 40),
            Font = new Font("Segoe UI", 12), BackColor = BgCard,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
            Text = "0,00", TextAlign = HorizontalAlignment.Right
        };
        txt.GotFocus += (s, e) => { if (txt.Text == "0,00") txt.Text = ""; };
        txt.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = "0,00"; };
        return txt;
    }

    private ComboBox CreateComboBox(int y, int width, int x = 0) => new()
    {
        Location = new Point(x, y), Size = new Size(width, 40),
        DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11),
        BackColor = BgCard, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
    };

    private Button CreateButton(string text, Color bg, int x, int width)
    {
        var btn = new Button
        {
            Text = text, Size = new Size(width, 45), Location = new Point(x, 20),
            FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11), Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);
        var service = new AccountService(unitOfWork);

        _accountTypes = (await service.GetAccountTypesAsync()).ToList();
        _cmbType.DataSource = _accountTypes;
        _cmbType.DisplayMember = "TypeName";
        _cmbType.ValueMember = "Id";

        if (_accountId.HasValue)
        {
            _account = await service.GetByIdAsync(_accountId.Value);
            if (_account != null)
            {
                _txtName.Text = _account.AccountName;
                _cmbType.SelectedValue = _account.AccountTypeId;
                _txtInitialBalance.Text = _account.InitialBalance.ToString("N2");
                _txtCreditLimit.Text = _account.CreditLimit.ToString("N2");
                _txtCutoffDay.Value = _account.CutoffDay;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Hesap adi zorunludur.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new AccountService(unitOfWork);

            if (_accountId.HasValue)
                _account = await service.GetByIdAsync(_accountId.Value);
            else
                _account = new Account { UserId = _userId };

            _account!.AccountName = _txtName.Text;
            _account.AccountTypeId = (int)(_cmbType.SelectedValue ?? 1);
            _account.CurrencyCode = _cmbCurrency.SelectedIndex switch
            {
                1 => "USD", 2 => "EUR", 3 => "GBP", _ => "TRY"
            };

            decimal.TryParse(_txtInitialBalance.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var initial);
            decimal.TryParse(_txtCreditLimit.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var limit);

            _account.InitialBalance = initial;
            _account.CreditLimit = limit;
            _account.CutoffDay = (int)_txtCutoffDay.Value;

            if (_accountId.HasValue)
                await service.UpdateAsync(_account);
            else
                await service.CreateAsync(_account);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayit hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
