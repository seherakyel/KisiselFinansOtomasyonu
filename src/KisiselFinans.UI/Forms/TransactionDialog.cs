using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class TransactionDialog : Form
{
    private readonly int _userId;
    private readonly byte _transactionType;
    private ComboBox _cmbAccount = null!;
    private ComboBox _cmbCategory = null!;
    private DateTimePicker _datePicker = null!;
    private TextBox _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private List<Account> _accounts = new();
    private List<Category> _categories = new();

    private const int DIALOG_WIDTH = 420;
    private const int DIALOG_HEIGHT = 560;
    private const int FIELD_WIDTH = 372;

    public TransactionDialog(int userId, byte transactionType)
    {
        _userId = userId;
        _transactionType = transactionType;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isIncome = _transactionType == 1;
        var accentColor = isIncome ? DialogStyles.AccentGreen : DialogStyles.AccentRed;
        var icon = isIncome ? "+" : "−";
        var title = isIncome ? "Gelir Ekle" : "Gider Ekle";
        var subtitle = isIncome ? "Yeni gelir kaydı oluşturun" : "Yeni gider kaydı oluşturun";

        DialogStyles.ApplyDialogStyle(this, DIALOG_WIDTH, DIALOG_HEIGHT);

        // Header
        var header = DialogStyles.CreateHeader(icon, title, subtitle, accentColor,
            () => { DialogResult = DialogResult.Cancel; Close(); });

        // Content
        var content = DialogStyles.CreateContentPanel();
        
        int y = 8;
        int spacing = 68;

        // Hesap
        content.Controls.Add(DialogStyles.CreateLabel("Hesap", 0, y));
        _cmbAccount = DialogStyles.CreateComboBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_cmbAccount);
        y += spacing;

        // Kategori
        content.Controls.Add(DialogStyles.CreateLabel("Kategori", 0, y));
        _cmbCategory = DialogStyles.CreateComboBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_cmbCategory);
        y += spacing;

        // Tutar
        content.Controls.Add(DialogStyles.CreateLabel("Tutar", 0, y));
        var (amountContainer, amountTxt) = DialogStyles.CreateCurrencyInput(0, y + 24, FIELD_WIDTH, accentColor);
        _txtAmount = amountTxt;
        content.Controls.Add(amountContainer);
        y += spacing + 8;

        // Tarih
        content.Controls.Add(DialogStyles.CreateLabel("Tarih", 0, y));
        _datePicker = DialogStyles.CreateDatePicker(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_datePicker);
        y += spacing;

        // Açıklama
        content.Controls.Add(DialogStyles.CreateLabel("Açıklama (Opsiyonel)", 0, y));
        _txtDescription = DialogStyles.CreateTextBox(0, y + 24, FIELD_WIDTH);
        _txtDescription.Height = 40;
        content.Controls.Add(_txtDescription);

        // Footer
        var footer = DialogStyles.CreateFooter(
            isIncome ? "Gelir Ekle" : "Gider Ekle",
            accentColor,
            () => { DialogResult = DialogResult.Cancel; Close(); },
            async () => await SaveAsync());

        // Divider
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

            var accountService = new AccountService(unitOfWork);
            var categoryService = new CategoryService(unitOfWork);

            _accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
            _categories = _transactionType == 1
                ? (await categoryService.GetIncomeCategories(_userId)).ToList()
                : (await categoryService.GetExpenseCategories(_userId)).ToList();

            BeginInvoke(() =>
            {
                _cmbAccount.DataSource = _accounts.Select(a => new { a.Id, Display = $"{a.AccountName}" }).ToList();
                _cmbAccount.DisplayMember = "Display";
                _cmbAccount.ValueMember = "Id";

                _cmbCategory.DataSource = _categories.Select(c => new { c.Id, Display = c.CategoryName }).ToList();
                _cmbCategory.DisplayMember = "Display";
                _cmbCategory.ValueMember = "Id";
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveAsync()
    {
        if (_cmbAccount.SelectedValue == null || _cmbCategory.SelectedValue == null)
        {
            ShowWarning("Lütfen hesap ve kategori seçiniz.");
            return;
        }

        if (!decimal.TryParse(_txtAmount.Text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount) || amount <= 0)
        {
            ShowWarning("Lütfen geçerli bir tutar giriniz.");
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);

            var dto = new CreateTransactionDto
            {
                AccountId = (int)_cmbAccount.SelectedValue,
                CategoryId = (int)_cmbCategory.SelectedValue,
                TransactionDate = _datePicker.Value,
                Amount = amount,
                TransactionType = _transactionType,
                Description = _txtDescription.Text.Trim()
            };

            await service.AddTransactionAsync(dto);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
