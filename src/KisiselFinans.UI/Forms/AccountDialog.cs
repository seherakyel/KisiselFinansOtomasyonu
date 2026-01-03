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
    private TextBox _txtInitialBalance = null!;
    private TextBox _txtCreditLimit = null!;
    private NumericUpDown _numCutoffDay = null!;
    private List<AccountType> _accountTypes = new();

    private const int DIALOG_WIDTH = 440;
    private const int DIALOG_HEIGHT = 520;
    private const int FIELD_WIDTH = 392;

    public AccountDialog(int userId, int? accountId = null)
    {
        _userId = userId;
        _accountId = accountId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _accountId.HasValue;
        DialogStyles.ApplyDialogStyle(this, DIALOG_WIDTH, DIALOG_HEIGHT);

        // Header
        var header = DialogStyles.CreateHeader(
            "🏦", isEdit ? "Hesap Düzenle" : "Yeni Hesap", "Hesap bilgilerini girin",
            DialogStyles.AccentPurple,
            () => { DialogResult = DialogResult.Cancel; Close(); });

        // Content
        var content = DialogStyles.CreateContentPanel();

        int y = 8;
        int spacing = 64;
        int halfWidth = (FIELD_WIDTH - 12) / 2;

        // Hesap Adı
        content.Controls.Add(DialogStyles.CreateLabel("Hesap Adı", 0, y));
        _txtName = DialogStyles.CreateTextBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_txtName);
        y += spacing;

        // Hesap Türü - Para Birimi (yan yana)
        content.Controls.Add(DialogStyles.CreateLabel("Hesap Türü", 0, y));
        _cmbType = DialogStyles.CreateComboBox(0, y + 24, halfWidth);
        content.Controls.Add(_cmbType);

        content.Controls.Add(DialogStyles.CreateLabel("Para Birimi", halfWidth + 12, y));
        _cmbCurrency = DialogStyles.CreateComboBox(halfWidth + 12, y + 24, halfWidth);
        _cmbCurrency.Items.AddRange(new[] { "TRY", "USD", "EUR", "GBP" });
        _cmbCurrency.SelectedIndex = 0;
        content.Controls.Add(_cmbCurrency);
        y += spacing;

        // Başlangıç Bakiyesi - Kredi Limiti (yan yana)
        content.Controls.Add(DialogStyles.CreateLabel("Başlangıç Bakiyesi", 0, y));
        var (balanceContainer, balanceTxt) = DialogStyles.CreateCurrencyInput(0, y + 24, halfWidth, DialogStyles.AccentGreen);
        _txtInitialBalance = balanceTxt;
        content.Controls.Add(balanceContainer);

        content.Controls.Add(DialogStyles.CreateLabel("Kredi Limiti", halfWidth + 12, y));
        var (limitContainer, limitTxt) = DialogStyles.CreateCurrencyInput(halfWidth + 12, y + 24, halfWidth, DialogStyles.AccentOrange);
        _txtCreditLimit = limitTxt;
        content.Controls.Add(limitContainer);
        y += spacing + 12;

        // Kesim Günü
        content.Controls.Add(DialogStyles.CreateLabel("Hesap Kesim Günü (Kredi Kartı)", 0, y));
        _numCutoffDay = new NumericUpDown
        {
            Location = new Point(0, y + 24),
            Size = new Size(100, 36),
            Minimum = 0,
            Maximum = 31,
            Font = new Font("Segoe UI", 11),
            BackColor = DialogStyles.BgInput,
            ForeColor = DialogStyles.TextWhite,
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(_numCutoffDay);

        // Footer
        var footer = DialogStyles.CreateFooter(
            "Kaydet",
            DialogStyles.AccentPurple,
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
            var service = new AccountService(unitOfWork);

            _accountTypes = (await service.GetAccountTypesAsync()).ToList();

            BeginInvoke(() =>
            {
                _cmbType.DataSource = _accountTypes;
                _cmbType.DisplayMember = "TypeName";
                _cmbType.ValueMember = "Id";
            });

            if (_accountId.HasValue)
            {
                _account = await service.GetByIdAsync(_accountId.Value);
                if (_account != null)
                {
                    BeginInvoke(() =>
                    {
                        _txtName.Text = _account.AccountName;
                        _cmbType.SelectedValue = _account.AccountTypeId;
                        _cmbCurrency.SelectedItem = _account.CurrencyCode;
                        _txtInitialBalance.Text = _account.InitialBalance.ToString("N2");
                        _txtCreditLimit.Text = _account.CreditLimit.ToString("N2");
                        _numCutoffDay.Value = _account.CutoffDay;
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
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Hesap adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new AccountService(unitOfWork);

            if (_accountId.HasValue)
                _account = await service.GetByIdAsync(_accountId.Value);
            else
                _account = new Account { UserId = _userId };

            _account!.AccountName = _txtName.Text.Trim();
            _account.AccountTypeId = (int)(_cmbType.SelectedValue ?? 1);
            _account.CurrencyCode = _cmbCurrency.SelectedItem?.ToString() ?? "TRY";

            decimal.TryParse(_txtInitialBalance.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var initial);
            decimal.TryParse(_txtCreditLimit.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var limit);

            _account.InitialBalance = initial;
            _account.CurrentBalance = _accountId.HasValue ? _account.CurrentBalance : initial;
            _account.CreditLimit = limit;
            _account.CutoffDay = (int)_numCutoffDay.Value;

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
