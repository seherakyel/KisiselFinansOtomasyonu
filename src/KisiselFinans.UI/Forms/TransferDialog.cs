using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class TransferDialog : Form
{
    private readonly int _userId;
    private ComboBox _cmbFromAccount = null!;
    private ComboBox _cmbToAccount = null!;
    private DateTimePicker _dateTransaction = null!;
    private TextBox _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private List<Account> _accounts = new();

    private static readonly Color AccentColor = Color.FromArgb(59, 130, 246);
    private static readonly Color AccentColorLight = Color.FromArgb(96, 165, 250);
    private static readonly Color BgDark = Color.FromArgb(17, 24, 39);
    private static readonly Color BgCard = Color.FromArgb(31, 41, 55);
    private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public TransferDialog(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = "Transfer";
        Size = new Size(480, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = BgDark;

        // Main container
        var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };

        // ===== HEADER =====
        var header = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = BgDark };

        // Icon
        var iconPanel = new Panel { Size = new Size(56, 56), Location = new Point(35, 17), BackColor = AccentColor };
        iconPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(AccentColor);
            e.Graphics.FillEllipse(brush, 0, 0, 55, 55);
            using var font = new Font("Segoe UI", 22, FontStyle.Bold);
            e.Graphics.DrawString("⇄", font, Brushes.White, 10, 10);
        };

        var lblTitle = new Label
        {
            Text = "Para Transferi",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(105, 22),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Hesaplar arasi para aktarimi",
            Font = new Font("Segoe UI", 11),
            ForeColor = TextMuted,
            Location = new Point(107, 52),
            AutoSize = true
        };

        var btnClose = CreateCloseButton();
        header.Controls.AddRange(new Control[] { iconPanel, lblTitle, lblSubtitle, btnClose });

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderColor };

        // ===== CONTENT =====
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(35, 25, 35, 25)
        };

        int y = 5;

        // FROM ACCOUNT
        content.Controls.Add(CreateInputSection("Kaynak Hesap", "Paranin cekilecegi hesap", y));
        _cmbFromAccount = CreateStyledComboBox(y + 45);
        content.Controls.Add(_cmbFromAccount);

        y += 95;

        // Arrow indicator
        var arrowPanel = new Panel
        {
            Location = new Point(175, y),
            Size = new Size(50, 30),
            BackColor = Color.Transparent
        };
        var arrowLabel = new Label
        {
            Text = "↓",
            Font = new Font("Segoe UI", 18),
            ForeColor = AccentColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        arrowPanel.Controls.Add(arrowLabel);
        content.Controls.Add(arrowPanel);

        y += 35;

        // TO ACCOUNT
        content.Controls.Add(CreateInputSection("Hedef Hesap", "Paranin yatirilacagi hesap", y));
        _cmbToAccount = CreateStyledComboBox(y + 45);
        content.Controls.Add(_cmbToAccount);

        y += 100;

        // AMOUNT
        content.Controls.Add(CreateInputSection("Transfer Tutari", "", y));

        var amountContainer = new Panel
        {
            Location = new Point(0, y + 40),
            Size = new Size(395, 55),
            BackColor = BgCard
        };

        var currencyLabel = new Label
        {
            Text = "₺",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = AccentColor,
            Size = new Size(50, 55),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = BgCard
        };

        _txtAmount = new TextBox
        {
            Location = new Point(50, 8),
            Size = new Size(340, 40),
            Font = new Font("Segoe UI", 18),
            BackColor = BgCard,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Text = "0,00",
            TextAlign = HorizontalAlignment.Right
        };
        _txtAmount.GotFocus += (s, e) => { if (_txtAmount.Text == "0,00") _txtAmount.Text = ""; };
        _txtAmount.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtAmount.Text)) _txtAmount.Text = "0,00"; };

        amountContainer.Controls.AddRange(new Control[] { currencyLabel, _txtAmount });
        content.Controls.Add(amountContainer);

        y += 110;

        // DATE
        content.Controls.Add(CreateInputSection("Tarih", "", y));
        _dateTransaction = new DateTimePicker
        {
            Location = new Point(0, y + 35),
            Size = new Size(395, 40),
            Format = DateTimePickerFormat.Long,
            Value = DateTime.Now,
            Font = new Font("Segoe UI", 11)
        };
        content.Controls.Add(_dateTransaction);

        y += 85;

        // DESCRIPTION
        content.Controls.Add(CreateInputSection("Not (Opsiyonel)", "", y));
        _txtDescription = new TextBox
        {
            Location = new Point(0, y + 35),
            Size = new Size(395, 45),
            Font = new Font("Segoe UI", 11),
            BackColor = BgCard,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = true
        };
        content.Controls.Add(_txtDescription);

        // ===== FOOTER =====
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 85,
            BackColor = Color.FromArgb(24, 32, 48),
            Padding = new Padding(35, 20, 35, 20)
        };

        var btnCancel = CreateButton("Vazgec", BorderColor, 140, false);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnTransfer = CreateButton("Transfer Yap  ⇄", AccentColor, 270, true);
        btnTransfer.Click += async (s, e) => await SaveAsync();

        footer.Controls.AddRange(new Control[] { btnCancel, btnTransfer });

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
            Text = "✕",
            Font = new Font("Segoe UI", 14),
            ForeColor = Color.FromArgb(100, 116, 139),
            Size = new Size(44, 44),
            Location = new Point(420, 10),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btn.MouseEnter += (s, e) => btn.ForeColor = Color.FromArgb(239, 68, 68);
        btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(100, 116, 139);
        return btn;
    }

    private Panel CreateInputSection(string label, string hint, int y)
    {
        var panel = new Panel { Location = new Point(0, y), Size = new Size(395, 45), BackColor = Color.Transparent };
        panel.Controls.Add(new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White,
            Location = new Point(0, 0),
            AutoSize = true
        });
        if (!string.IsNullOrEmpty(hint))
        {
            panel.Controls.Add(new Label
            {
                Text = hint,
                Font = new Font("Segoe UI", 9),
                ForeColor = TextMuted,
                Location = new Point(0, 22),
                AutoSize = true
            });
        }
        return panel;
    }

    private ComboBox CreateStyledComboBox(int y) => new()
    {
        Location = new Point(0, y),
        Size = new Size(395, 45),
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 12),
        BackColor = BgCard,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
    };

    private Button CreateButton(string text, Color bgColor, int x, bool isPrimary)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(isPrimary ? 160 : 120, 45),
            Location = new Point(x, 20),
            FlatStyle = FlatStyle.Flat,
            BackColor = bgColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        if (isPrimary)
        {
            btn.MouseEnter += (s, e) => btn.BackColor = AccentColorLight;
            btn.MouseLeave += (s, e) => btn.BackColor = bgColor;
        }
        return btn;
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);
        var accountService = new AccountService(unitOfWork);

        _accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();

        _cmbFromAccount.DataSource = _accounts.ToList();
        _cmbFromAccount.DisplayMember = "AccountName";
        _cmbFromAccount.ValueMember = "Id";

        _cmbToAccount.DataSource = _accounts.ToList();
        _cmbToAccount.DisplayMember = "AccountName";
        _cmbToAccount.ValueMember = "Id";
    }

    private async Task SaveAsync()
    {
        if (_cmbFromAccount.SelectedValue == null || _cmbToAccount.SelectedValue == null)
        {
            MessageBox.Show("Kaynak ve hedef hesap seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if ((int)_cmbFromAccount.SelectedValue == (int)_cmbToAccount.SelectedValue)
        {
            MessageBox.Show("Kaynak ve hedef hesap ayni olamaz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(_txtAmount.Text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount) || amount <= 0)
        {
            MessageBox.Show("Gecerli bir tutar giriniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);

            var dto = new TransferDto
            {
                FromAccountId = (int)_cmbFromAccount.SelectedValue,
                ToAccountId = (int)_cmbToAccount.SelectedValue,
                TransactionDate = _dateTransaction.Value,
                Amount = amount,
                Description = _txtDescription.Text
            };

            await service.TransferAsync(dto);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Transfer hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
