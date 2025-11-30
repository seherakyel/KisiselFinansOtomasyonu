using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class BudgetDialog : XtraForm
{
    private readonly int _userId;
    private readonly int? _budgetId;
    private Budget? _budget;

    private LookUpEdit _cmbCategory = null!;
    private SpinEdit _txtLimit = null!;
    private DateEdit _dateStart = null!;
    private DateEdit _dateEnd = null!;

    public BudgetDialog(int userId, int? budgetId)
    {
        _userId = userId;
        _budgetId = budgetId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _budgetId.HasValue ? "Bütçe Düzenle" : "Yeni Bütçe";
        Size = new Size(400, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var lblCategory = new LabelControl { Text = "Kategori", Location = new Point(20, 20) };
        _cmbCategory = new LookUpEdit { Location = new Point(20, 40), Size = new Size(330, 28) };
        _cmbCategory.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CategoryName", "Kategori"));
        _cmbCategory.Properties.DisplayMember = "CategoryName";
        _cmbCategory.Properties.ValueMember = "Id";

        var lblLimit = new LabelControl { Text = "Limit", Location = new Point(20, 75) };
        _txtLimit = new SpinEdit { Location = new Point(20, 95), Size = new Size(330, 28) };
        _txtLimit.Properties.DisplayFormat.FormatString = "N2";

        var lblStart = new LabelControl { Text = "Başlangıç Tarihi", Location = new Point(20, 130) };
        _dateStart = new DateEdit
        {
            Location = new Point(20, 150),
            Size = new Size(155, 28),
            EditValue = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
        };

        var lblEnd = new LabelControl { Text = "Bitiş Tarihi", Location = new Point(195, 130) };
        _dateEnd = new DateEdit
        {
            Location = new Point(195, 150),
            Size = new Size(155, 28),
            EditValue = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
        };

        var btnSave = new SimpleButton
        {
            Text = "Kaydet",
            Location = new Point(170, 210),
            Size = new Size(90, 30),
            Appearance = { BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White }
        };
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new SimpleButton { Text = "İptal", Location = new Point(265, 210), Size = new Size(90, 30) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblCategory, _cmbCategory, lblLimit, _txtLimit,
            lblStart, _dateStart, lblEnd, _dateEnd, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);

        var categoryService = new CategoryService(unitOfWork);
        var budgetService = new BudgetService(unitOfWork);

        _cmbCategory.Properties.DataSource = (await categoryService.GetExpenseCategories(_userId)).ToList();

        if (_budgetId.HasValue)
        {
            _budget = await budgetService.GetByIdAsync(_budgetId.Value);
            if (_budget != null)
            {
                _cmbCategory.EditValue = _budget.CategoryId;
                _txtLimit.Value = _budget.AmountLimit;
                _dateStart.EditValue = _budget.StartDate;
                _dateEnd.EditValue = _budget.EndDate;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbCategory.EditValue == null)
        {
            XtraMessageBox.Show("Kategori seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if ((decimal)_txtLimit.Value <= 0)
        {
            XtraMessageBox.Show("Limit sıfırdan büyük olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new BudgetService(unitOfWork);

            if (_budget == null)
            {
                _budget = new Budget { UserId = _userId };
            }
            else
            {
                _budget = await service.GetByIdAsync(_budgetId!.Value);
            }

            _budget!.CategoryId = (int)_cmbCategory.EditValue;
            _budget.AmountLimit = (decimal)_txtLimit.Value;
            _budget.StartDate = (DateTime)_dateStart.EditValue;
            _budget.EndDate = (DateTime)_dateEnd.EditValue;

            if (_budgetId.HasValue)
                await service.UpdateAsync(_budget);
            else
                await service.CreateAsync(_budget);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

