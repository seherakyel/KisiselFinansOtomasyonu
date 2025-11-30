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
    private DateTimePicker _dateTransaction = null!;
    private NumericUpDown _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private List<Account> _accounts = new();

    public TransferDialog(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = "🔄 Hesaplar Arası Transfer";
        Size = new Size(450, 400);
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
        const int spacing = 60;

        var lblFrom = CreateLabel("Kaynak Hesap", y);
        _cmbFromAccount = CreateComboBox(y + 20);

        y += spacing;
        var lblTo = CreateLabel("Hedef Hesap", y);
        _cmbToAccount = CreateComboBox(y + 20);

        y += spacing;
        var lblDate = CreateLabel("Tarih", y);
        _dateTransaction = new DateTimePicker
        {
            Location = new Point(0, y + 20),
            Size = new Size(180, 32),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now
        };

        var lblAmount = CreateLabel("Tutar", y, 200);
        _txtAmount = new NumericUpDown
        {
            Location = new Point(200, y + 20),
            Size = new Size(180, 32),
            Maximum = 999999999,
            DecimalPlaces = 2,
            ThousandsSeparator = true
        };
        AppTheme.StyleNumericUpDown(_txtAmount);

        y += spacing;
        var lblDesc = CreateLabel("Açıklama", y);
        _txtDescription = new TextBox
        {
            Location = new Point(0, y + 20),
            Size = new Size(380, 60),
            Multiline = true
        };
        AppTheme.StyleTextBox(_txtDescription);

        y += 90;
        var btnSave = new Button
        {
            Text = "🔄 TRANSFER ET",
            Location = new Point(160, y),
            Size = new Size(120, 38)
        };
        AppTheme.StyleButton(btnSave, true);
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
            lblFrom, _cmbFromAccount, lblTo, _cmbToAccount,
            lblDate, _dateTransaction, lblAmount, _txtAmount,
            lblDesc, _txtDescription, btnSave, btnCancel
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

    private ComboBox CreateComboBox(int y)
    {
        var cmb = new ComboBox
        {
            Location = new Point(0, y),
            Size = new Size(380, 32),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        AppTheme.StyleComboBox(cmb);
        return cmb;
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);
        var accountService = new AccountService(unitOfWork);

        _accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        
        _cmbFromAccount.DataSource = _accounts.ToList();
        _cmbFromAccount.DisplayMember = "AccountName";
        _cmbFromAccount.ValueMember = "Id";

        _cmbToAccount.DataSource = _accounts.ToList();
        _cmbToAccount.DisplayMember = "AccountName";
        _cmbToAccount.ValueMember = "Id";
    }

    private async Task SaveAsync()
    {
        if (_cmbFromAccount.SelectedValue == null || _cmbToAccount.SelectedValue == null)
        {
            MessageBox.Show("Kaynak ve hedef hesap seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if ((int)_cmbFromAccount.SelectedValue == (int)_cmbToAccount.SelectedValue)
        {
            MessageBox.Show("Kaynak ve hedef hesap aynı olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_txtAmount.Value <= 0)
        {
            MessageBox.Show("Tutar sıfırdan büyük olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);

            var dto = new TransferDto
            {
                FromAccountId = (int)_cmbFromAccount.SelectedValue,
                ToAccountId = (int)_cmbToAccount.SelectedValue,
                TransactionDate = _dateTransaction.Value,
                Amount = _txtAmount.Value,
                Description = _txtDescription.Text
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
}
