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
    private NumericUpDown _txtLimit = null!;
    private DateTimePicker _dateStart = null!;
    private DateTimePicker _dateEnd = null!;
    private List<Category> _categories = new();

    public BudgetDialog(int userId, int? budgetId)
    {
        _userId = userId;
        _budgetId = budgetId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _budgetId.HasValue ? "🎯 Bütçe Düzenle" : "🎯 Yeni Bütçe";
        Size = new Size(400, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.PrimaryDark;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30),
            BackColor = AppTheme.PrimaryDark
        };

        int y = 10;
        const int spacing = 55;

        var lblCategory = CreateLabel("Kategori", y);
        _cmbCategory = new ComboBox
        {
            Location = new Point(0, y + 20),
            Size = new Size(320, 32),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        AppTheme.StyleComboBox(_cmbCategory);

        y += spacing;
        var lblLimit = CreateLabel("Limit", y);
        _txtLimit = new NumericUpDown
        {
            Location = new Point(0, y + 20),
            Size = new Size(320, 32),
            Maximum = 999999999,
            DecimalPlaces = 2,
            ThousandsSeparator = true
        };
        AppTheme.StyleNumericUpDown(_txtLimit);

        y += spacing;
        var lblStart = CreateLabel("Başlangıç Tarihi", y);
        _dateStart = new DateTimePicker
        {
            Location = new Point(0, y + 20),
            Size = new Size(155, 32),
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
        };

        var lblEnd = CreateLabel("Bitiş Tarihi", y, 170);
        _dateEnd = new DateTimePicker
        {
            Location = new Point(170, y + 20),
            Size = new Size(150, 32),
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
        };

        y += spacing + 15;
        var btnSave = new Button
        {
            Text = "💾 KAYDET",
            Location = new Point(140, y),
            Size = new Size(90, 38)
        };
        AppTheme.StyleSuccessButton(btnSave);
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new Button
        {
            Text = "İPTAL",
            Location = new Point(240, y),
            Size = new Size(80, 38)
        };
        AppTheme.StyleButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblCategory, _cmbCategory, lblLimit, _txtLimit,
            lblStart, _dateStart, lblEnd, _dateEnd, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private Label CreateLabel(string text, int y, int x = 0) => new()
    {
        Text = text,
        Font = AppTheme.FontSmall,
        ForeColor = AppTheme.TextSecondary,
        Location = new Point(x, y),
        AutoSize = true
    };

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
                _txtLimit.Value = _budget.AmountLimit;
                _dateStart.Value = _budget.StartDate;
                _dateEnd.Value = _budget.EndDate;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbCategory.SelectedValue == null)
        {
            MessageBox.Show("Kategori seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_txtLimit.Value <= 0)
        {
            MessageBox.Show("Limit sıfırdan büyük olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new BudgetService(unitOfWork);

            if (_budgetId.HasValue)
            {
                _budget = await service.GetByIdAsync(_budgetId.Value);
            }
            else
            {
                _budget = new Budget { UserId = _userId };
            }

            _budget!.CategoryId = (int)_cmbCategory.SelectedValue;
            _budget.AmountLimit = _txtLimit.Value;
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
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
