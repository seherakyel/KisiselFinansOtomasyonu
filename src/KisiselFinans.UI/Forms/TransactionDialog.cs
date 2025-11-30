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
    private DateTimePicker _dateTransaction = null!;
    private NumericUpDown _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private List<Account> _accounts = new();
    private List<Category> _categories = new();

    public TransactionDialog(int userId, byte transactionType)
    {
        _userId = userId;
        _transactionType = transactionType;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _transactionType == 1 ? "💵 Gelir Ekle" : "💸 Gider Ekle";
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

        var lblAccount = CreateLabel("Hesap", y);
        _cmbAccount = CreateComboBox(y + 20);

        y += spacing;
        var lblCategory = CreateLabel("Kategori", y);
        _cmbCategory = CreateComboBox(y + 20);

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
            lblAccount, _cmbAccount, lblCategory, _cmbCategory,
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
        var categoryService = new CategoryService(unitOfWork);

        _accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        _cmbAccount.DataSource = _accounts;
        _cmbAccount.DisplayMember = "AccountName";
        _cmbAccount.ValueMember = "Id";

        _categories = _transactionType == 1
            ? (await categoryService.GetIncomeCategories(_userId)).ToList()
            : (await categoryService.GetExpenseCategories(_userId)).ToList();
        _cmbCategory.DataSource = _categories;
        _cmbCategory.DisplayMember = "CategoryName";
        _cmbCategory.ValueMember = "Id";
    }

    private async Task SaveAsync()
    {
        if (_cmbAccount.SelectedValue == null || _cmbCategory.SelectedValue == null)
        {
            MessageBox.Show("Hesap ve kategori seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            var dto = new CreateTransactionDto
            {
                AccountId = (int)_cmbAccount.SelectedValue,
                CategoryId = (int)_cmbCategory.SelectedValue,
                TransactionDate = _dateTransaction.Value,
                Amount = _txtAmount.Value,
                TransactionType = _transactionType,
                Description = _txtDescription.Text
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
}
