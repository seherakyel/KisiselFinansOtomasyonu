using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class AccountDialog : Form
{
    private readonly int _userId;
    private readonly int? _accountId;
    private Account? _account;

    private TextBox _txtName = null!;
    private ComboBox _cmbType = null!;
    private ComboBox _cmbCurrency = null!;
    private NumericUpDown _txtInitialBalance = null!;
    private NumericUpDown _txtCreditLimit = null!;
    private NumericUpDown _txtCutoffDay = null!;
    private List<AccountType> _accountTypes = new();

    public AccountDialog(int userId, int? accountId)
    {
        _userId = userId;
        _accountId = accountId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _accountId.HasValue ? "🏦 Hesap Düzenle" : "🏦 Yeni Hesap";
        Size = new Size(450, 450);
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

        var lblName = CreateLabel("Hesap Adı", y);
        _txtName = new TextBox { Location = new Point(0, y + 20), Size = new Size(380, 32) };
        AppTheme.StyleTextBox(_txtName);

        y += spacing;
        var lblType = CreateLabel("Hesap Türü", y);
        _cmbType = new ComboBox
        {
            Location = new Point(0, y + 20),
            Size = new Size(180, 32),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        AppTheme.StyleComboBox(_cmbType);

        var lblCurrency = CreateLabel("Para Birimi", y, 200);
        _cmbCurrency = new ComboBox
        {
            Location = new Point(200, y + 20),
            Size = new Size(180, 32),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbCurrency.Items.AddRange(new[] { "TRY", "USD", "EUR", "GBP", "XAU" });
        _cmbCurrency.SelectedIndex = 0;
        AppTheme.StyleComboBox(_cmbCurrency);

        y += spacing;
        var lblInitial = CreateLabel("Başlangıç Bakiyesi", y);
        _txtInitialBalance = new NumericUpDown
        {
            Location = new Point(0, y + 20),
            Size = new Size(180, 32),
            Maximum = 999999999,
            Minimum = -999999999,
            DecimalPlaces = 2
        };
        AppTheme.StyleNumericUpDown(_txtInitialBalance);

        var lblLimit = CreateLabel("Kredi Limiti", y, 200);
        _txtCreditLimit = new NumericUpDown
        {
            Location = new Point(200, y + 20),
            Size = new Size(180, 32),
            Maximum = 999999999,
            DecimalPlaces = 2
        };
        AppTheme.StyleNumericUpDown(_txtCreditLimit);

        y += spacing;
        var lblCutoff = CreateLabel("Hesap Kesim Günü (Kredi Kartı)", y);
        _txtCutoffDay = new NumericUpDown
        {
            Location = new Point(0, y + 20),
            Size = new Size(180, 32),
            Minimum = 0,
            Maximum = 31
        };
        AppTheme.StyleNumericUpDown(_txtCutoffDay);

        y += spacing + 20;
        var btnSave = new Button
        {
            Text = "💾 KAYDET",
            Location = new Point(190, y),
            Size = new Size(90, 38)
        };
        AppTheme.StyleSuccessButton(btnSave);
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new Button
        {
            Text = "İPTAL",
            Location = new Point(290, y),
            Size = new Size(90, 38)
        };
        AppTheme.StyleButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblName, _txtName, lblType, _cmbType, lblCurrency, _cmbCurrency,
            lblInitial, _txtInitialBalance, lblLimit, _txtCreditLimit,
            lblCutoff, _txtCutoffDay, btnSave, btnCancel
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
        var service = new AccountService(unitOfWork);

        _accountTypes = (await service.GetAccountTypesAsync()).ToList();
        _cmbType.DataSource = _accountTypes;
        _cmbType.DisplayMember = "TypeName";
        _cmbType.ValueMember = "Id";

        if (_accountId.HasValue)
        {
            _account = await service.GetByIdAsync(_accountId.Value);
            if (_account != null)
            {
                _txtName.Text = _account.AccountName;
                _cmbType.SelectedValue = _account.AccountTypeId;
                _cmbCurrency.SelectedItem = _account.CurrencyCode;
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
            MessageBox.Show("Hesap adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cmbType.SelectedValue == null)
        {
            MessageBox.Show("Hesap türü seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new AccountService(unitOfWork);

            if (_accountId.HasValue)
            {
                _account = await service.GetByIdAsync(_accountId.Value);
            }
            else
            {
                _account = new Account { UserId = _userId };
            }

            _account!.AccountName = _txtName.Text;
            _account.AccountTypeId = (int)_cmbType.SelectedValue;
            _account.CurrencyCode = _cmbCurrency.SelectedItem?.ToString() ?? "TRY";
            _account.InitialBalance = _txtInitialBalance.Value;
            _account.CreditLimit = _txtCreditLimit.Value;
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
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
