using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class ScheduledTransactionDialog : XtraForm
{
    private readonly int _userId;
    private readonly int? _scheduledId;
    private ScheduledTransaction? _scheduled;

    private LookUpEdit _cmbAccount = null!;
    private LookUpEdit _cmbCategory = null!;
    private SpinEdit _txtAmount = null!;
    private MemoEdit _txtDescription = null!;
    private ComboBoxEdit _cmbFrequency = null!;
    private SpinEdit _txtDayOfMonth = null!;
    private DateEdit _dateNext = null!;
    private CheckEdit _chkActive = null!;

    public ScheduledTransactionDialog(int userId, int? scheduledId)
    {
        _userId = userId;
        _scheduledId = scheduledId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _scheduledId.HasValue ? "Planlı İşlem Düzenle" : "Yeni Planlı İşlem";
        Size = new Size(450, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var lblAccount = new LabelControl { Text = "Hesap", Location = new Point(20, 20) };
        _cmbAccount = new LookUpEdit { Location = new Point(20, 40), Size = new Size(380, 28) };
        _cmbAccount.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AccountName", "Hesap"));
        _cmbAccount.Properties.DisplayMember = "AccountName";
        _cmbAccount.Properties.ValueMember = "Id";

        var lblCategory = new LabelControl { Text = "Kategori", Location = new Point(20, 75) };
        _cmbCategory = new LookUpEdit { Location = new Point(20, 95), Size = new Size(380, 28) };
        _cmbCategory.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CategoryName", "Kategori"));
        _cmbCategory.Properties.DisplayMember = "CategoryName";
        _cmbCategory.Properties.ValueMember = "Id";

        var lblAmount = new LabelControl { Text = "Tutar", Location = new Point(20, 130) };
        _txtAmount = new SpinEdit { Location = new Point(20, 150), Size = new Size(180, 28) };
        _txtAmount.Properties.DisplayFormat.FormatString = "N2";

        var lblFrequency = new LabelControl { Text = "Sıklık", Location = new Point(220, 130) };
        _cmbFrequency = new ComboBoxEdit { Location = new Point(220, 150), Size = new Size(180, 28) };
        _cmbFrequency.Properties.Items.AddRange(new[] { "Daily", "Weekly", "Monthly", "Yearly" });
        _cmbFrequency.SelectedIndex = 2;

        var lblDay = new LabelControl { Text = "Ayın Günü", Location = new Point(20, 185) };
        _txtDayOfMonth = new SpinEdit { Location = new Point(20, 205), Size = new Size(180, 28) };
        _txtDayOfMonth.Properties.MinValue = 1;
        _txtDayOfMonth.Properties.MaxValue = 31;
        _txtDayOfMonth.Value = DateTime.Now.Day;

        var lblNext = new LabelControl { Text = "Sonraki Tarih", Location = new Point(220, 185) };
        _dateNext = new DateEdit { Location = new Point(220, 205), Size = new Size(180, 28), EditValue = DateTime.Now.AddMonths(1) };

        var lblDesc = new LabelControl { Text = "Açıklama", Location = new Point(20, 240) };
        _txtDescription = new MemoEdit { Location = new Point(20, 260), Size = new Size(380, 50) };

        _chkActive = new CheckEdit { Text = "Aktif", Location = new Point(20, 320), Checked = true };

        var btnSave = new SimpleButton
        {
            Text = "Kaydet",
            Location = new Point(220, 370),
            Size = new Size(90, 30),
            Appearance = { BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White }
        };
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new SimpleButton { Text = "İptal", Location = new Point(315, 370), Size = new Size(90, 30) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblAccount, _cmbAccount, lblCategory, _cmbCategory,
            lblAmount, _txtAmount, lblFrequency, _cmbFrequency,
            lblDay, _txtDayOfMonth, lblNext, _dateNext,
            lblDesc, _txtDescription, _chkActive, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);

        var accountService = new AccountService(unitOfWork);
        var categoryService = new CategoryService(unitOfWork);
        var scheduledService = new ScheduledTransactionService(unitOfWork);

        _cmbAccount.Properties.DataSource = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        _cmbCategory.Properties.DataSource = (await categoryService.GetUserCategoriesAsync(_userId)).ToList();

        if (_scheduledId.HasValue)
        {
            _scheduled = await scheduledService.GetByIdAsync(_scheduledId.Value);
            if (_scheduled != null)
            {
                _cmbAccount.EditValue = _scheduled.AccountId;
                _cmbCategory.EditValue = _scheduled.CategoryId;
                _txtAmount.Value = _scheduled.Amount;
                _cmbFrequency.EditValue = _scheduled.FrequencyType;
                _txtDayOfMonth.Value = _scheduled.DayOfMonth ?? 1;
                _dateNext.EditValue = _scheduled.NextExecutionDate;
                _txtDescription.Text = _scheduled.Description;
                _chkActive.Checked = _scheduled.IsActive;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbAccount.EditValue == null || _cmbCategory.EditValue == null)
        {
            XtraMessageBox.Show("Hesap ve kategori seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if ((decimal)_txtAmount.Value <= 0)
        {
            XtraMessageBox.Show("Tutar sıfırdan büyük olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new ScheduledTransactionService(unitOfWork);

            if (_scheduled == null)
            {
                _scheduled = new ScheduledTransaction { UserId = _userId };
            }
            else
            {
                _scheduled = await service.GetByIdAsync(_scheduledId!.Value);
            }

            _scheduled!.AccountId = (int)_cmbAccount.EditValue;
            _scheduled.CategoryId = (int)_cmbCategory.EditValue;
            _scheduled.Amount = (decimal)_txtAmount.Value;
            _scheduled.FrequencyType = _cmbFrequency.EditValue?.ToString() ?? "Monthly";
            _scheduled.DayOfMonth = (int)_txtDayOfMonth.Value;
            _scheduled.NextExecutionDate = (DateTime)_dateNext.EditValue;
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
            XtraMessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

