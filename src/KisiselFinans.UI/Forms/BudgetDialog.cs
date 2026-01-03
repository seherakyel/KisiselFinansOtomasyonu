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

    private const int DIALOG_WIDTH = 420;
    private const int DIALOG_HEIGHT = 480;
    private const int FIELD_WIDTH = 372;

    public BudgetDialog(int userId, int? budgetId = null)
    {
        _userId = userId;
        _budgetId = budgetId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _budgetId.HasValue;
        DialogStyles.ApplyDialogStyle(this, DIALOG_WIDTH, DIALOG_HEIGHT);

        // Header
        var header = DialogStyles.CreateHeader(
            "🎯", isEdit ? "Bütçe Düzenle" : "Yeni Bütçe", "Harcama limitinizi belirleyin",
            DialogStyles.AccentOrange,
            () => { DialogResult = DialogResult.Cancel; Close(); });

        // Content
        var content = DialogStyles.CreateContentPanel();

        int y = 8;
        int spacing = 72;
        int halfWidth = (FIELD_WIDTH - 12) / 2;

        // Kategori
        content.Controls.Add(DialogStyles.CreateLabel("Kategori", 0, y));
        _cmbCategory = DialogStyles.CreateComboBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_cmbCategory);
        y += spacing;

        // Aylık Limit
        content.Controls.Add(DialogStyles.CreateLabel("Aylık Harcama Limiti", 0, y));
        var (limitContainer, limitTxt) = DialogStyles.CreateCurrencyInput(0, y + 24, FIELD_WIDTH, DialogStyles.AccentOrange);
        _txtLimit = limitTxt;
        content.Controls.Add(limitContainer);
        y += spacing + 8;

        // Tarih Aralığı (yan yana)
        content.Controls.Add(DialogStyles.CreateLabel("Başlangıç Tarihi", 0, y));
        _dateStart = DialogStyles.CreateDatePicker(0, y + 24, halfWidth);
        _dateStart.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        content.Controls.Add(_dateStart);

        content.Controls.Add(DialogStyles.CreateLabel("Bitiş Tarihi", halfWidth + 12, y));
        _dateEnd = DialogStyles.CreateDatePicker(halfWidth + 12, y + 24, halfWidth);
        _dateEnd.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
        content.Controls.Add(_dateEnd);

        // Footer
        var footer = DialogStyles.CreateFooter(
            isEdit ? "Güncelle" : "Oluştur",
            DialogStyles.AccentOrange,
            () => { DialogResult = DialogResult.Cancel; Close(); },
            async () => await SaveAsync());

        var divider = DialogStyles.CreateDivider();

        Controls.Add(content);
        Controls.Add(divider);
        Controls.Add(header);
        Controls.Add(footer);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);

            var categoryService = new CategoryService(unitOfWork);
            var budgetService = new BudgetService(unitOfWork);

            _categories = (await categoryService.GetExpenseCategories(_userId)).ToList();

            BeginInvoke(() =>
            {
                _cmbCategory.DataSource = _categories;
                _cmbCategory.DisplayMember = "CategoryName";
                _cmbCategory.ValueMember = "Id";
            });

            if (_budgetId.HasValue)
            {
                _budget = await budgetService.GetByIdAsync(_budgetId.Value);
                if (_budget != null)
                {
                    BeginInvoke(() =>
                    {
                        _cmbCategory.SelectedValue = _budget.CategoryId;
                        _txtLimit.Text = _budget.AmountLimit.ToString("N2");
                        _dateStart.Value = _budget.StartDate;
                        _dateEnd.Value = _budget.EndDate;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbCategory.SelectedValue == null)
        {
            MessageBox.Show("Lütfen kategori seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(_txtLimit.Text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var limit) || limit <= 0)
        {
            MessageBox.Show("Lütfen geçerli bir limit giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            _budget.StartDate = _dateStart.Value.Date;
            _budget.EndDate = _dateEnd.Value.Date;

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
