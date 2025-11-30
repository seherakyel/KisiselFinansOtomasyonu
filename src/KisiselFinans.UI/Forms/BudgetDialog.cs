using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class BudgetDialog : Form
{
    private readonly int _userId;
    private readonly int? _budgetId;
    private Budget? _budget;

    private ComboBox _cmbCategory = null!;
    private TextBox _txtLimit = null!;
    private DateTimePicker _dateStart = null!;
    private DateTimePicker _dateEnd = null!;
    private List<Category> _categories = new();

    private static readonly Color AccentColor = Color.FromArgb(245, 158, 11);
    private static readonly Color AccentLight = Color.FromArgb(251, 191, 36);
    private static readonly Color BgDark = Color.FromArgb(17, 24, 39);
    private static readonly Color BgCard = Color.FromArgb(31, 41, 55);
    private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public BudgetDialog(int userId, int? budgetId)
    {
        _userId = userId;
        _budgetId = budgetId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _budgetId.HasValue;
        Text = isEdit ? "Butce Duzenle" : "Yeni Butce";
        Size = new Size(480, 500);
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
            e.Graphics.DrawString("🎯", font, Brushes.White, 8, 8);
        };

        header.Controls.Add(iconPanel);
        header.Controls.Add(new Label
        {
            Text = isEdit ? "Butce Duzenle" : "Yeni Butce",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(105, 22),
            AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = "Harcama limitinizi belirleyin",
            Font = new Font("Segoe UI", 11),
            ForeColor = TextMuted,
            Location = new Point(107, 52),
            AutoSize = true
        });
        header.Controls.Add(CreateCloseButton());

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderColor };

        // ===== CONTENT =====
        var content = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(35, 25, 35, 25) };

        int y = 0;

        // KATEGORI
        content.Controls.Add(CreateLabel("Kategori", y));
        content.Controls.Add(CreateHint("Bu butce hangi kategoriye uygulanacak?", y + 22));
        _cmbCategory = CreateComboBox(y + 48);
        content.Controls.Add(_cmbCategory);

        y += 95;

        // LIMIT
        content.Controls.Add(CreateLabel("Aylik Limit", y));
        content.Controls.Add(CreateHint("Bu kategoride aylik maksimum harcama", y + 22));

        var limitContainer = new Panel { Location = new Point(0, y + 48), Size = new Size(390, 55), BackColor = BgCard };
        var currencyLabel = new Label
        {
            Text = "₺", Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = AccentColor, Size = new Size(50, 55),
            TextAlign = ContentAlignment.MiddleCenter, BackColor = BgCard
        };
        _txtLimit = new TextBox
        {
            Location = new Point(50, 8), Size = new Size(335, 40),
            Font = new Font("Segoe UI", 18), BackColor = BgCard,
            ForeColor = Color.White, BorderStyle = BorderStyle.None,
            Text = "0,00", TextAlign = HorizontalAlignment.Right
        };
        _txtLimit.GotFocus += (s, e) => { if (_txtLimit.Text == "0,00") _txtLimit.Text = ""; };
        _txtLimit.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtLimit.Text)) _txtLimit.Text = "0,00"; };
        limitContainer.Controls.AddRange(new Control[] { currencyLabel, _txtLimit });
        content.Controls.Add(limitContainer);

        y += 115;

        // TARIH ARALIGI
        content.Controls.Add(CreateLabel("Gecerlilik Tarihleri", y));

        var dateContainer = new Panel { Location = new Point(0, y + 28), Size = new Size(390, 65), BackColor = Color.Transparent };
        
        dateContainer.Controls.Add(new Label { Text = "Baslangic", Font = new Font("Segoe UI", 9), ForeColor = TextMuted, Location = new Point(0, 0), AutoSize = true });
        _dateStart = new DateTimePicker
        {
            Location = new Point(0, 20), Size = new Size(185, 40),
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            Font = new Font("Segoe UI", 11)
        };
        dateContainer.Controls.Add(_dateStart);

        dateContainer.Controls.Add(new Label { Text = "Bitis", Font = new Font("Segoe UI", 9), ForeColor = TextMuted, Location = new Point(205, 0), AutoSize = true });
        _dateEnd = new DateTimePicker
        {
            Location = new Point(205, 20), Size = new Size(185, 40),
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
            Font = new Font("Segoe UI", 11)
        };
        dateContainer.Controls.Add(_dateEnd);
        content.Controls.Add(dateContainer);

        // ===== FOOTER =====
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 85, BackColor = Color.FromArgb(24, 32, 48) };

        var btnCancel = CreateButton("Vazgec", BorderColor, 155, 120);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = CreateButton("Butce Olustur", AccentColor, 285, 140);
        btnSave.ForeColor = BgDark;
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

    private Label CreateLabel(string text, int y) => new()
    {
        Text = text, Font = new Font("Segoe UI Semibold", 11),
        ForeColor = Color.White, Location = new Point(0, y), AutoSize = true
    };

    private Label CreateHint(string text, int y) => new()
    {
        Text = text, Font = new Font("Segoe UI", 9),
        ForeColor = TextMuted, Location = new Point(0, y), AutoSize = true
    };

    private ComboBox CreateComboBox(int y) => new()
    {
        Location = new Point(0, y), Size = new Size(390, 45),
        DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12),
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

        var categoryService = new CategoryService(unitOfWork);
        var budgetService = new BudgetService(unitOfWork);

        _categories = (await categoryService.GetExpenseCategories(_userId)).ToList();
        _cmbCategory.DataSource = _categories;
        _cmbCategory.DisplayMember = "CategoryName";
        _cmbCategory.ValueMember = "Id";

        if (_budgetId.HasValue)
        {
            _budget = await budgetService.GetByIdAsync(_budgetId.Value);
            if (_budget != null)
            {
                _cmbCategory.SelectedValue = _budget.CategoryId;
                _txtLimit.Text = _budget.AmountLimit.ToString("N2");
                _dateStart.Value = _budget.StartDate;
                _dateEnd.Value = _budget.EndDate;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbCategory.SelectedValue == null)
        {
            MessageBox.Show("Kategori seciniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(_txtLimit.Text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var limit) || limit <= 0)
        {
            MessageBox.Show("Gecerli bir limit giriniz.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new BudgetService(unitOfWork);

            if (_budgetId.HasValue)
                _budget = await service.GetByIdAsync(_budgetId.Value);
            else
                _budget = new Budget { UserId = _userId };

            _budget!.CategoryId = (int)_cmbCategory.SelectedValue;
            _budget.AmountLimit = limit;
            _budget.StartDate = _dateStart.Value;
            _budget.EndDate = _dateEnd.Value;

            if (_budgetId.HasValue)
                await service.UpdateAsync(_budget);
            else
                await service.CreateAsync(_budget);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayit hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
