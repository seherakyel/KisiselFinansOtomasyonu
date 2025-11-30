using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class ScheduledTransactionDialog : Form
{
    private readonly int _userId;
    private readonly int? _scheduledId;
    private ScheduledTransaction? _scheduled;

    private ComboBox _cmbAccount = null!;
    private ComboBox _cmbCategory = null!;
    private TextBox _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private ComboBox _cmbFrequency = null!;
    private NumericUpDown _txtDayOfMonth = null!;
    private DateTimePicker _dateNext = null!;
    private CheckBox _chkActive = null!;

    private static readonly Color AccentColor = Color.FromArgb(6, 182, 212);
    private static readonly Color BgDark = Color.FromArgb(17, 24, 39);
    private static readonly Color BgCard = Color.FromArgb(31, 41, 55);
    private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public ScheduledTransactionDialog(int userId, int? scheduledId)
    {
        _userId = userId;
        _scheduledId = scheduledId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _scheduledId.HasValue;
        Text = isEdit ? "Planli Islem Duzenle" : "Yeni Planli Islem";
        Size = new Size(500, 650);
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
            e.Graphics.DrawString("📅", font, Brushes.White, 8, 8);
        };

        header.Controls.Add(iconPanel);
        header.Controls.Add(new Label
        {
            Text = isEdit ? "Planli Islem Duzenle" : "Yeni Planli Islem",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(105, 22),
            AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = "Otomatik tekrarlayan islem olusturun",
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted,
            Location = new Point(107, 50),
            AutoSize = true
        });
        header.Controls.Add(CreateCloseButton());

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderColor };

        // ===== CONTENT =====
        var content = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(35, 20, 35, 15), AutoScroll = true };

        int y = 0;

        // HESAP
        content.Controls.Add(CreateLabel("Hesap", y));
        _cmbAccount = CreateComboBox(y + 25);
        content.Controls.Add(_cmbAccount);

        y += 70;

        // KATEGORI
        content.Controls.Add(CreateLabel("Kategori", y));
        _cmbCategory = CreateComboBox(y + 25);
        content.Controls.Add(_cmbCategory);

        y += 70;

        // TUTAR & SIKLIK
        content.Controls.Add(CreateLabel("Tutar", y));
        var amountContainer = new Panel { Location = new Point(0, y + 25), Size = new Size(200, 45), BackColor = BgCard };
        var currencyLabel = new Label
        {
            Text = "₺", Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = AccentColor, Size = new Size(40, 45),
            TextAlign = ContentAlignment.MiddleCenter, BackColor = BgCard
        };
        _txtAmount = new TextBox
        {
            Location = new Point(40, 5), Size = new Size(155, 35),
            Font = new Font("Segoe UI", 14), BackColor = BgCard,
            ForeColor = Color.White, BorderStyle = BorderStyle.None,
            Text = "0,00", TextAlign = HorizontalAlignment.Right
        };
        amountContainer.Controls.AddRange(new Control[] { currencyLabel, _txtAmount });
        content.Controls.Add(amountContainer);

        content.Controls.Add(CreateLabel("Tekrar Sikligi", y, 220));
        _cmbFrequency = new ComboBox
        {
            Location = new Point(220, y + 25), Size = new Size(195, 45),
            DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11),
            BackColor = BgCard, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        _cmbFrequency.Items.AddRange(new[] { "Gunluk", "Haftalik", "Aylik", "Yillik" });
        _cmbFrequency.SelectedIndex = 2;
        content.Controls.Add(_cmbFrequency);

        y += 75;

        // GUN & SONRAKI TARIH
        content.Controls.Add(CreateLabel("Ayin Gunu", y));
        _txtDayOfMonth = new NumericUpDown
        {
            Location = new Point(0, y + 25), Size = new Size(100, 40),
            Minimum = 1, Maximum = 31, Value = DateTime.Now.Day,
            Font = new Font("Segoe UI", 12), BackColor = BgCard,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(_txtDayOfMonth);

        content.Controls.Add(CreateLabel("Sonraki Islem Tarihi", y, 120));
        _dateNext = new DateTimePicker
        {
            Location = new Point(120, y + 25), Size = new Size(295, 40),
            Format = DateTimePickerFormat.Long, Value = DateTime.Now.AddMonths(1),
            Font = new Font("Segoe UI", 11)
        };
        content.Controls.Add(_dateNext);

        y += 75;

        // ACIKLAMA
        content.Controls.Add(CreateLabel("Aciklama (Opsiyonel)", y));
        _txtDescription = new TextBox
        {
            Location = new Point(0, y + 25), Size = new Size(415, 50),
            Font = new Font("Segoe UI", 11), BackColor = BgCard,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Multiline = true
        };
        content.Controls.Add(_txtDescription);

        y += 90;

        // AKTIF SWITCH
        var activePanel = new Panel { Location = new Point(0, y), Size = new Size(415, 40), BackColor = BgCard };
        _chkActive = new CheckBox
        {
            Text = "  Bu planli islem aktif", Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White, Location = new Point(15, 8), AutoSize = true, Checked = true
        };
        activePanel.Controls.Add(_chkActive);
        content.Controls.Add(activePanel);

        // ===== FOOTER =====
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 85, BackColor = Color.FromArgb(24, 32, 48) };

        var btnCancel = new Button
        {
            Text = "Vazgec", Size = new Size(120, 45), Location = new Point(170, 20),
            FlatStyle = FlatStyle.Flat, BackColor = BorderColor, ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11), Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = new Button
        {
            Text = "Planla", Size = new Size(140, 45), Location = new Point(300, 20),
            FlatStyle = FlatStyle.Flat, BackColor = AccentColor, ForeColor = BgDark,
            Font = new Font("Segoe UI Semibold", 11), Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
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
            Size = new Size(44, 44), Location = new Point(440, 10),
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

    private ComboBox CreateComboBox(int y) => new()
    {
        Location = new Point(0, y), Size = new Size(415, 40),
        DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11),
        BackColor = BgCard, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
    };

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);

        var accountService = new AccountService(unitOfWork);
        var categoryService = new CategoryService(unitOfWork);
        var scheduledService = new ScheduledTransactionService(unitOfWork);

        var accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        _cmbAccount.DataSource = accounts;
        _cmbAccount.DisplayMember = "AccountName";
        _cmbAccount.ValueMember = "Id";

        var categories = (await categoryService.GetUserCategoriesAsync(_userId)).ToList();
        _cmbCategory.DataSource = categories;
        _cmbCategory.DisplayMember = "CategoryName";
        _cmbCategory.ValueMember = "Id";

        if (_scheduledId.HasValue)
        {
            _scheduled = await scheduledService.GetByIdAsync(_scheduledId.Value);
            if (_scheduled != null)
            {
                _cmbAccount.SelectedValue = _scheduled.AccountId;
                _cmbCategory.SelectedValue = _scheduled.CategoryId;
                _txtAmount.Text = _scheduled.Amount.ToString("N2");
                _cmbFrequency.SelectedItem = _scheduled.FrequencyType switch
                {
                    "Daily" => "Gunluk", "Weekly" => "Haftalik",
                    "Yearly" => "Yillik", _ => "Aylik"
                };
                _txtDayOfMonth.Value = _scheduled.DayOfMonth ?? 1;
                _dateNext.Value = _scheduled.NextExecutionDate;
                _txtDescription.Text = _scheduled.Description;
                _chkActive.Checked = _scheduled.IsActive;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbAccount.SelectedValue == null || _cmbCategory.SelectedValue == null)
        {
            MessageBox.Show("Hesap ve kategori seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            var service = new ScheduledTransactionService(unitOfWork);

            if (_scheduledId.HasValue)
                _scheduled = await service.GetByIdAsync(_scheduledId.Value);
            else
                _scheduled = new ScheduledTransaction { UserId = _userId };

            _scheduled!.AccountId = (int)_cmbAccount.SelectedValue;
            _scheduled.CategoryId = (int)_cmbCategory.SelectedValue;
            _scheduled.Amount = amount;
            _scheduled.FrequencyType = _cmbFrequency.SelectedIndex switch
            {
                0 => "Daily", 1 => "Weekly", 3 => "Yearly", _ => "Monthly"
            };
            _scheduled.DayOfMonth = (int)_txtDayOfMonth.Value;
            _scheduled.NextExecutionDate = _dateNext.Value;
            _scheduled.Description = _txtDescription.Text;
            _scheduled.IsActive = _chkActive.Checked;

            if (_scheduledId.HasValue)
                await service.UpdateAsync(_scheduled);
            else
                await service.CreateAsync(_scheduled);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayit hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
