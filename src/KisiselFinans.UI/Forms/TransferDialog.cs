using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class TransferDialog : Form
{
    private readonly int _userId;
    private ComboBox _cmbFromAccount = null!;
    private ComboBox _cmbToAccount = null!;
    private DateTimePicker _datePicker = null!;
    private TextBox _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private List<Account> _accounts = new();

    private const int DIALOG_WIDTH = 420;
    private const int DIALOG_HEIGHT = 540;
    private const int CONTENT_PADDING = 24;
    private const int FIELD_WIDTH = 372;

    public TransferDialog(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        DialogStyles.ApplyDialogStyle(this, DIALOG_WIDTH, DIALOG_HEIGHT);

        // Header
        var header = DialogStyles.CreateHeader(
            "⇄", "Para Transferi", "Hesaplar arası para aktarımı",
            DialogStyles.AccentBlue,
            () => { DialogResult = DialogResult.Cancel; Close(); });

        // Content
        var content = DialogStyles.CreateContentPanel();
        
        int y = 8;
        int spacing = 68;

        // Kaynak Hesap
        content.Controls.Add(DialogStyles.CreateLabel("Kaynak Hesap", 0, y));
        _cmbFromAccount = DialogStyles.CreateComboBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_cmbFromAccount);
        y += spacing;

        // Transfer göstergesi
        var arrowPanel = new Panel
        {
            Location = new Point(FIELD_WIDTH / 2 - 16, y),
            Size = new Size(32, 32),
            BackColor = Color.Transparent
        };
        var arrowLabel = new Label
        {
            Text = "↓",
            Font = new Font("Segoe UI", 16),
            ForeColor = DialogStyles.AccentBlue,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        arrowPanel.Controls.Add(arrowLabel);
        content.Controls.Add(arrowPanel);
        y += 40;

        // Hedef Hesap
        content.Controls.Add(DialogStyles.CreateLabel("Hedef Hesap", 0, y));
        _cmbToAccount = DialogStyles.CreateComboBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_cmbToAccount);
        y += spacing;

        // Transfer Tutarı
        content.Controls.Add(DialogStyles.CreateLabel("Transfer Tutarı", 0, y));
        var (amountContainer, amountTxt) = DialogStyles.CreateCurrencyInput(0, y + 24, FIELD_WIDTH, DialogStyles.AccentBlue);
        _txtAmount = amountTxt;
        content.Controls.Add(amountContainer);
        y += spacing + 8;

        // Tarih
        content.Controls.Add(DialogStyles.CreateLabel("İşlem Tarihi", 0, y));
        _datePicker = DialogStyles.CreateDatePicker(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_datePicker);
        y += spacing;

        // Açıklama
        content.Controls.Add(DialogStyles.CreateLabel("Not (Opsiyonel)", 0, y));
        _txtDescription = DialogStyles.CreateTextBox(0, y + 24, FIELD_WIDTH);
        _txtDescription.Height = 40;
        content.Controls.Add(_txtDescription);

        // Footer
        var footer = DialogStyles.CreateFooter(
            "Transfer Yap",
            DialogStyles.AccentBlue,
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

            _accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();

            BeginInvoke(() =>
            {
                _cmbFromAccount.DataSource = _accounts.Select(a => new { a.Id, Display = $"{a.AccountName} (₺{a.CurrentBalance:N2})" }).ToList();
                _cmbFromAccount.DisplayMember = "Display";
                _cmbFromAccount.ValueMember = "Id";

                _cmbToAccount.DataSource = _accounts.Select(a => new { a.Id, Display = $"{a.AccountName} (₺{a.CurrentBalance:N2})" }).ToList();
                _cmbToAccount.DisplayMember = "Display";
                _cmbToAccount.ValueMember = "Id";
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveAsync()
    {
        // Validasyon
        if (_cmbFromAccount.SelectedValue == null || _cmbToAccount.SelectedValue == null)
        {
            ShowWarning("Lütfen kaynak ve hedef hesap seçiniz.");
            return;
        }

        var fromId = (int)_cmbFromAccount.SelectedValue;
        var toId = (int)_cmbToAccount.SelectedValue;

        if (fromId == toId)
        {
            ShowWarning("Kaynak ve hedef hesap aynı olamaz.");
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

            var dto = new TransferDto
            {
                FromAccountId = fromId,
                ToAccountId = toId,
                TransactionDate = _datePicker.Value,
                Amount = amount,
                Description = _txtDescription.Text.Trim()
            };

            await service.TransferAsync(dto);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Transfer hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
