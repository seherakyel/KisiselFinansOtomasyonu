using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class TransactionDialog : XtraForm
{
    private readonly int _userId;
    private readonly byte _transactionType;
    private LookUpEdit _cmbAccount = null!;
    private LookUpEdit _cmbCategory = null!;
    private DateEdit _dateTransaction = null!;
    private SpinEdit _txtAmount = null!;
    private MemoEdit _txtDescription = null!;

    public TransactionDialog(int userId, byte transactionType)
    {
        _userId = userId;
        _transactionType = transactionType;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _transactionType == 1 ? "Gelir Ekle" : "Gider Ekle";
        Size = new Size(450, 350);
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

        var lblDate = new LabelControl { Text = "Tarih", Location = new Point(20, 130) };
        _dateTransaction = new DateEdit { Location = new Point(20, 150), Size = new Size(180, 28), EditValue = DateTime.Now };

        var lblAmount = new LabelControl { Text = "Tutar", Location = new Point(220, 130) };
        _txtAmount = new SpinEdit { Location = new Point(220, 150), Size = new Size(180, 28) };
        _txtAmount.Properties.DisplayFormat.FormatString = "N2";
        _txtAmount.Properties.EditFormat.FormatString = "N2";

        var lblDesc = new LabelControl { Text = "Açıklama", Location = new Point(20, 185) };
        _txtDescription = new MemoEdit { Location = new Point(20, 205), Size = new Size(380, 60) };

        var btnSave = new SimpleButton
        {
            Text = "Kaydet",
            Location = new Point(220, 280),
            Size = new Size(90, 30),
            Appearance = { BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White }
        };
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new SimpleButton { Text = "İptal", Location = new Point(315, 280), Size = new Size(90, 30) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblAccount, _cmbAccount, lblCategory, _cmbCategory,
            lblDate, _dateTransaction, lblAmount, _txtAmount,
            lblDesc, _txtDescription, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);

        var accountService = new AccountService(unitOfWork);
        var categoryService = new CategoryService(unitOfWork);

        _cmbAccount.Properties.DataSource = (await accountService.GetUserAccountsAsync(_userId)).ToList();

        var categories = _transactionType == 1
            ? await categoryService.GetIncomeCategories(_userId)
            : await categoryService.GetExpenseCategories(_userId);
        _cmbCategory.Properties.DataSource = categories.ToList();
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
            var service = new TransactionService(unitOfWork);

            var dto = new CreateTransactionDto
            {
                AccountId = (int)_cmbAccount.EditValue,
                CategoryId = (int)_cmbCategory.EditValue,
                TransactionDate = (DateTime)_dateTransaction.EditValue,
                Amount = (decimal)_txtAmount.Value,
                TransactionType = _transactionType,
                Description = _txtDescription.Text
            };

            await service.AddTransactionAsync(dto);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

