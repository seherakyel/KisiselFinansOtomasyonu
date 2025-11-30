using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class AccountDialog : XtraForm
{
    private readonly int _userId;
    private readonly int? _accountId;
    private Account? _account;

    private TextEdit _txtName = null!;
    private LookUpEdit _cmbType = null!;
    private ComboBoxEdit _cmbCurrency = null!;
    private SpinEdit _txtInitialBalance = null!;
    private SpinEdit _txtCreditLimit = null!;
    private SpinEdit _txtCutoffDay = null!;

    public AccountDialog(int userId, int? accountId)
    {
        _userId = userId;
        _accountId = accountId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _accountId.HasValue ? "Hesap Düzenle" : "Yeni Hesap";
        Size = new Size(450, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var lblName = new LabelControl { Text = "Hesap Adı", Location = new Point(20, 20) };
        _txtName = new TextEdit { Location = new Point(20, 40), Size = new Size(380, 28) };

        var lblType = new LabelControl { Text = "Hesap Türü", Location = new Point(20, 75) };
        _cmbType = new LookUpEdit { Location = new Point(20, 95), Size = new Size(180, 28) };
        _cmbType.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("TypeName", "Tür"));
        _cmbType.Properties.DisplayMember = "TypeName";
        _cmbType.Properties.ValueMember = "Id";

        var lblCurrency = new LabelControl { Text = "Para Birimi", Location = new Point(220, 75) };
        _cmbCurrency = new ComboBoxEdit { Location = new Point(220, 95), Size = new Size(180, 28) };
        _cmbCurrency.Properties.Items.AddRange(new[] { "TRY", "USD", "EUR", "GBP", "XAU" });
        _cmbCurrency.SelectedIndex = 0;

        var lblInitial = new LabelControl { Text = "Başlangıç Bakiyesi", Location = new Point(20, 130) };
        _txtInitialBalance = new SpinEdit { Location = new Point(20, 150), Size = new Size(180, 28) };
        _txtInitialBalance.Properties.DisplayFormat.FormatString = "N2";

        var lblLimit = new LabelControl { Text = "Kredi Limiti (Kart için)", Location = new Point(220, 130) };
        _txtCreditLimit = new SpinEdit { Location = new Point(220, 150), Size = new Size(180, 28) };
        _txtCreditLimit.Properties.DisplayFormat.FormatString = "N2";

        var lblCutoff = new LabelControl { Text = "Hesap Kesim Günü", Location = new Point(20, 185) };
        _txtCutoffDay = new SpinEdit { Location = new Point(20, 205), Size = new Size(180, 28) };
        _txtCutoffDay.Properties.MinValue = 0;
        _txtCutoffDay.Properties.MaxValue = 31;

        var btnSave = new SimpleButton
        {
            Text = "Kaydet",
            Location = new Point(220, 320),
            Size = new Size(90, 30),
            Appearance = { BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White }
        };
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new SimpleButton { Text = "İptal", Location = new Point(315, 320), Size = new Size(90, 30) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblName, _txtName, lblType, _cmbType, lblCurrency, _cmbCurrency,
            lblInitial, _txtInitialBalance, lblLimit, _txtCreditLimit,
            lblCutoff, _txtCutoffDay, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);
        var service = new AccountService(unitOfWork);

        _cmbType.Properties.DataSource = (await service.GetAccountTypesAsync()).ToList();

        if (_accountId.HasValue)
        {
            _account = await service.GetByIdAsync(_accountId.Value);
            if (_account != null)
            {
                _txtName.Text = _account.AccountName;
                _cmbType.EditValue = _account.AccountTypeId;
                _cmbCurrency.EditValue = _account.CurrencyCode;
                _txtInitialBalance.Value = _account.InitialBalance;
                _txtCreditLimit.Value = _account.CreditLimit;
                _txtCutoffDay.Value = _account.CutoffDay;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            XtraMessageBox.Show("Hesap adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cmbType.EditValue == null)
        {
            XtraMessageBox.Show("Hesap türü seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new AccountService(unitOfWork);

            if (_account == null)
            {
                _account = new Account { UserId = _userId };
            }
            else
            {
                _account = await service.GetByIdAsync(_accountId!.Value);
            }

            _account!.AccountName = _txtName.Text;
            _account.AccountTypeId = (int)_cmbType.EditValue;
            _account.CurrencyCode = _cmbCurrency.EditValue?.ToString() ?? "TRY";
            _account.InitialBalance = (decimal)_txtInitialBalance.Value;
            _account.CreditLimit = (decimal)_txtCreditLimit.Value;
            _account.CutoffDay = (int)_txtCutoffDay.Value;

            if (_accountId.HasValue)
                await service.UpdateAsync(_account);
            else
                await service.CreateAsync(_account);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

