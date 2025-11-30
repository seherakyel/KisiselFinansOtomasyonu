using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class TransactionDialog : Form
{
    private readonly int _userId;
    private readonly byte _transactionType;
    private ComboBox _cmbAccount = null!;
    private ComboBox _cmbCategory = null!;
    private DateTimePicker _dateTransaction = null!;
    private TextBox _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private List<Account> _accounts = new();
    private List<Category> _categories = new();

    public TransactionDialog(int userId, byte transactionType)
    {
        _userId = userId;
        _transactionType = transactionType;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isIncome = _transactionType == 1;
        var accentColor = isIncome ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
        var accentColorLight = isIncome ? Color.FromArgb(52, 211, 153) : Color.FromArgb(248, 113, 113);

        Text = isIncome ? "Gelir Ekle" : "Gider Ekle";
        Size = new Size(480, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(17, 24, 39);
        Padding = new Padding(1);

        // Border effect
        var borderPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(0)
        };

        // Main container
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39)
        };

        // ===== HEADER =====
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Color.FromArgb(17, 24, 39)
        };

        // Icon circle
        var iconPanel = new Panel
        {
            Size = new Size(56, 56),
            Location = new Point(35, 17),
            BackColor = accentColor
        };
        iconPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(accentColor);
            e.Graphics.FillEllipse(brush, 0, 0, 55, 55);
            
            var icon = isIncome ? "+" : "-";
            using var font = new Font("Segoe UI", 28, FontStyle.Bold);
            var size = e.Graphics.MeasureString(icon, font);
            e.Graphics.DrawString(icon, font, Brushes.White, 
                (55 - size.Width) / 2, (55 - size.Height) / 2);
        };

        var lblTitle = new Label
        {
            Text = isIncome ? "Yeni Gelir" : "Yeni Gider",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(105, 22),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = isIncome ? "Gelir detaylarini girin" : "Gider detaylarini girin",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(107, 52),
            AutoSize = true
        };

        var btnClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 14),
            ForeColor = Color.FromArgb(100, 116, 139),
            Size = new Size(44, 44),
            Location = new Point(420, 10),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68);
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(100, 116, 139);

        header.Controls.AddRange(new Control[] { iconPanel, lblTitle, lblSubtitle, btnClose });

        // Divider
        var divider = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(55, 65, 81)
        };

        // ===== CONTENT =====
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(35, 25, 35, 25)
        };

        int y = 5;

        // HESAP
        var accountSection = CreateInputSection("Hesap", "Islem yapilacak hesabi secin", y);
        _cmbAccount = CreateStyledComboBox(y + 45);
        content.Controls.Add(accountSection);
        content.Controls.Add(_cmbAccount);

        y += 95;

        // KATEGORI
        var categorySection = CreateInputSection("Kategori", "Islem kategorisini secin", y);
        _cmbCategory = CreateStyledComboBox(y + 45);
        content.Controls.Add(categorySection);
        content.Controls.Add(_cmbCategory);

        y += 95;

        // TUTAR - Large and prominent
        var amountSection = CreateInputSection("Tutar", "Islem tutarini girin", y);
        content.Controls.Add(amountSection);

        var amountContainer = new Panel
        {
            Location = new Point(0, y + 45),
            Size = new Size(395, 55),
            BackColor = Color.FromArgb(31, 41, 55)
        };

        var currencyLabel = new Label
        {
            Text = "₺",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = accentColor,
            Size = new Size(50, 55),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(31, 41, 55)
        };

        _txtAmount = new TextBox
        {
            Location = new Point(50, 8),
            Size = new Size(340, 40),
            Font = new Font("Segoe UI", 18),
            BackColor = Color.FromArgb(31, 41, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Text = "0,00",
            TextAlign = HorizontalAlignment.Right
        };
        _txtAmount.GotFocus += (s, e) => { if (_txtAmount.Text == "0,00") _txtAmount.Text = ""; };
        _txtAmount.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtAmount.Text)) _txtAmount.Text = "0,00"; };
        _txtAmount.KeyPress += (s, e) =>
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != (char)Keys.Back)
                e.Handled = true;
        };

        amountContainer.Controls.AddRange(new Control[] { currencyLabel, _txtAmount });
        content.Controls.Add(amountContainer);

        y += 110;

        // TARIH
        var dateSection = CreateInputSection("Tarih", "", y);
        content.Controls.Add(dateSection);

        _dateTransaction = new DateTimePicker
        {
            Location = new Point(0, y + 40),
            Size = new Size(395, 45),
            Format = DateTimePickerFormat.Long,
            Value = DateTime.Now,
            Font = new Font("Segoe UI", 12),
            CalendarForeColor = Color.White,
            CalendarMonthBackground = Color.FromArgb(31, 41, 55)
        };
        content.Controls.Add(_dateTransaction);

        y += 90;

        // ACIKLAMA
        var descSection = CreateInputSection("Not (Opsiyonel)", "", y);
        content.Controls.Add(descSection);

        _txtDescription = new TextBox
        {
            Location = new Point(0, y + 40),
            Size = new Size(395, 50),
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(31, 41, 55),
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

        var btnCancel = new Button
        {
            Text = "Vazgec",
            Size = new Size(120, 45),
            Location = new Point(140, 20),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 65, 81),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = new Button
        {
            Text = isIncome ? "Gelir Ekle  +" : "Gider Ekle  -",
            Size = new Size(160, 45),
            Location = new Point(270, 20),
            FlatStyle = FlatStyle.Flat,
            BackColor = accentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveAsync();
        btnSave.MouseEnter += (s, e) => btnSave.BackColor = accentColorLight;
        btnSave.MouseLeave += (s, e) => btnSave.BackColor = accentColor;

        footer.Controls.AddRange(new Control[] { btnCancel, btnSave });

        // Add top accent line
        var accentLine = new Panel
        {
            Dock = DockStyle.Top,
            Height = 3,
            BackColor = accentColor
        };

        mainPanel.Controls.Add(content);
        mainPanel.Controls.Add(divider);
        mainPanel.Controls.Add(header);
        mainPanel.Controls.Add(footer);
        mainPanel.Controls.Add(accentLine);

        borderPanel.Controls.Add(mainPanel);
        Controls.Add(borderPanel);
    }

    private Panel CreateInputSection(string label, string hint, int y)
    {
        var panel = new Panel
        {
            Location = new Point(0, y),
            Size = new Size(395, 45),
            BackColor = Color.Transparent
        };

        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White,
            Location = new Point(0, 0),
            AutoSize = true
        };

        if (!string.IsNullOrEmpty(hint))
        {
            var lblHint = new Label
            {
                Text = hint,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, 22),
                AutoSize = true
            };
            panel.Controls.Add(lblHint);
        }

        panel.Controls.Add(lbl);
        return panel;
    }

    private ComboBox CreateStyledComboBox(int y)
    {
        var cmb = new ComboBox
        {
            Location = new Point(0, y),
            Size = new Size(395, 45),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 12),
            BackColor = Color.FromArgb(31, 41, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        return cmb;
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);

        var accountService = new AccountService(unitOfWork);
        var categoryService = new CategoryService(unitOfWork);

        _accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        _cmbAccount.DataSource = _accounts;
        _cmbAccount.DisplayMember = "AccountName";
        _cmbAccount.ValueMember = "Id";

        _categories = _transactionType == 1
            ? (await categoryService.GetIncomeCategories(_userId)).ToList()
            : (await categoryService.GetExpenseCategories(_userId)).ToList();
        _cmbCategory.DataSource = _categories;
        _cmbCategory.DisplayMember = "CategoryName";
        _cmbCategory.ValueMember = "Id";
    }

    private async Task SaveAsync()
    {
        if (_cmbAccount.SelectedValue == null || _cmbCategory.SelectedValue == null)
        {
            ShowError("Hesap ve kategori seciniz.");
            return;
        }

        if (!decimal.TryParse(_txtAmount.Text.Replace(",", "."), 
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, 
            out var amount) || amount <= 0)
        {
            ShowError("Gecerli bir tutar giriniz.");
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);

            var dto = new CreateTransactionDto
            {
                AccountId = (int)_cmbAccount.SelectedValue,
                CategoryId = (int)_cmbCategory.SelectedValue,
                TransactionDate = _dateTransaction.Value,
                Amount = amount,
                TransactionType = _transactionType,
                Description = _txtDescription.Text
            };

            await service.AddTransactionAsync(dto);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Kayit hatasi: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
