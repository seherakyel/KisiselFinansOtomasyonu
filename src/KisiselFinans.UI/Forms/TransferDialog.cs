using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class TransferDialog : XtraForm
{
    private readonly int _userId;
    private LookUpEdit _cmbFromAccount = null!;
    private LookUpEdit _cmbToAccount = null!;
    private DateEdit _dateTransaction = null!;
    private SpinEdit _txtAmount = null!;
    private MemoEdit _txtDescription = null!;

    public TransferDialog(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = "Hesaplar Arası Transfer";
        Size = new Size(450, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var lblFrom = new LabelControl { Text = "Kaynak Hesap", Location = new Point(20, 20) };
        _cmbFromAccount = new LookUpEdit { Location = new Point(20, 40), Size = new Size(380, 28) };
        _cmbFromAccount.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AccountName", "Hesap"));
        _cmbFromAccount.Properties.DisplayMember = "AccountName";
        _cmbFromAccount.Properties.ValueMember = "Id";

        var lblTo = new LabelControl { Text = "Hedef Hesap", Location = new Point(20, 75) };
        _cmbToAccount = new LookUpEdit { Location = new Point(20, 95), Size = new Size(380, 28) };
        _cmbToAccount.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AccountName", "Hesap"));
        _cmbToAccount.Properties.DisplayMember = "AccountName";
        _cmbToAccount.Properties.ValueMember = "Id";

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
            Text = "Transfer Et",
            Location = new Point(200, 280),
            Size = new Size(100, 30),
            Appearance = { BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White }
        };
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new SimpleButton { Text = "İptal", Location = new Point(310, 280), Size = new Size(90, 30) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblFrom, _cmbFromAccount, lblTo, _cmbToAccount,
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

        var accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        _cmbFromAccount.Properties.DataSource = accounts;
        _cmbToAccount.Properties.DataSource = accounts;
    }

    private async Task SaveAsync()
    {
        if (_cmbFromAccount.EditValue == null || _cmbToAccount.EditValue == null)
        {
            XtraMessageBox.Show("Kaynak ve hedef hesap seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if ((int)_cmbFromAccount.EditValue == (int)_cmbToAccount.EditValue)
        {
            XtraMessageBox.Show("Kaynak ve hedef hesap aynı olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            var dto = new TransferDto
            {
                FromAccountId = (int)_cmbFromAccount.EditValue,
                ToAccountId = (int)_cmbToAccount.EditValue,
                TransactionDate = (DateTime)_dateTransaction.EditValue,
                Amount = (decimal)_txtAmount.Value,
                Description = _txtDescription.Text
            };

            await service.TransferAsync(dto);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Transfer hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

