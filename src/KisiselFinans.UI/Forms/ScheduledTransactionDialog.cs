using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class ScheduledTransactionDialog : Form
{
    private readonly int _userId;
    private readonly int? _scheduledId;
    private ScheduledTransaction? _scheduled;

    private ComboBox _cmbAccount = null!;
    private ComboBox _cmbCategory = null!;
    private NumericUpDown _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private ComboBox _cmbFrequency = null!;
    private NumericUpDown _txtDayOfMonth = null!;
    private DateTimePicker _dateNext = null!;
    private CheckBox _chkActive = null!;

    public ScheduledTransactionDialog(int userId, int? scheduledId)
    {
        _userId = userId;
        _scheduledId = scheduledId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _scheduledId.HasValue ? "📅 Planlı İşlem Düzenle" : "📅 Yeni Planlı İşlem";
        Size = new Size(450, 500);
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
        const int spacing = 50;

        var lblAccount = CreateLabel("Hesap", y);
        _cmbAccount = CreateComboBox(y + 18);

        y += spacing;
        var lblCategory = CreateLabel("Kategori", y);
        _cmbCategory = CreateComboBox(y + 18);

        y += spacing;
        var lblAmount = CreateLabel("Tutar", y);
        _txtAmount = new NumericUpDown
        {
            Location = new Point(0, y + 18),
            Size = new Size(180, 30),
            Maximum = 999999999,
            DecimalPlaces = 2
        };
        AppTheme.StyleNumericUpDown(_txtAmount);

        var lblFrequency = CreateLabel("Sıklık", y, 200);
        _cmbFrequency = new ComboBox
        {
            Location = new Point(200, y + 18),
            Size = new Size(180, 30),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbFrequency.Items.AddRange(new[] { "Daily", "Weekly", "Monthly", "Yearly" });
        _cmbFrequency.SelectedIndex = 2;
        AppTheme.StyleComboBox(_cmbFrequency);

        y += spacing;
        var lblDay = CreateLabel("Ayın Günü", y);
        _txtDayOfMonth = new NumericUpDown
        {
            Location = new Point(0, y + 18),
            Size = new Size(180, 30),
            Minimum = 1,
            Maximum = 31,
            Value = DateTime.Now.Day
        };
        AppTheme.StyleNumericUpDown(_txtDayOfMonth);

        var lblNext = CreateLabel("Sonraki Tarih", y, 200);
        _dateNext = new DateTimePicker
        {
            Location = new Point(200, y + 18),
            Size = new Size(180, 30),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddMonths(1)
        };

        y += spacing;
        var lblDesc = CreateLabel("Açıklama", y);
        _txtDescription = new TextBox
        {
            Location = new Point(0, y + 18),
            Size = new Size(380, 50),
            Multiline = true
        };
        AppTheme.StyleTextBox(_txtDescription);

        y += 70;
        _chkActive = new CheckBox
        {
            Text = "Aktif",
            Location = new Point(0, y),
            ForeColor = AppTheme.TextPrimary,
            Checked = true,
            AutoSize = true
        };

        y += 40;
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
            lblAmount, _txtAmount, lblFrequency, _cmbFrequency,
            lblDay, _txtDayOfMonth, lblNext, _dateNext,
            lblDesc, _txtDescription, _chkActive, btnSave, btnCancel
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
            Size = new Size(380, 30),
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
        var scheduledService = new ScheduledTransactionService(unitOfWork);

        var accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
        _cmbAccount.DataSource = accounts;
        _cmbAccount.DisplayMember = "AccountName";
        _cmbAccount.ValueMember = "Id";

        var categories = (await categoryService.GetUserCategoriesAsync(_userId)).ToList();
        _cmbCategory.DataSource = categories;
        _cmbCategory.DisplayMember = "CategoryName";
        _cmbCategory.ValueMember = "Id";

        if (_scheduledId.HasValue)
        {
            _scheduled = await scheduledService.GetByIdAsync(_scheduledId.Value);
            if (_scheduled != null)
            {
                _cmbAccount.SelectedValue = _scheduled.AccountId;
                _cmbCategory.SelectedValue = _scheduled.CategoryId;
                _txtAmount.Value = _scheduled.Amount;
                _cmbFrequency.SelectedItem = _scheduled.FrequencyType;
                _txtDayOfMonth.Value = _scheduled.DayOfMonth ?? 1;
                _dateNext.Value = _scheduled.NextExecutionDate;
                _txtDescription.Text = _scheduled.Description;
                _chkActive.Checked = _scheduled.IsActive;
            }
        }
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
            var service = new ScheduledTransactionService(unitOfWork);

            if (_scheduledId.HasValue)
            {
                _scheduled = await service.GetByIdAsync(_scheduledId.Value);
            }
            else
            {
                _scheduled = new ScheduledTransaction { UserId = _userId };
            }

            _scheduled!.AccountId = (int)_cmbAccount.SelectedValue;
            _scheduled.CategoryId = (int)_cmbCategory.SelectedValue;
            _scheduled.Amount = _txtAmount.Value;
            _scheduled.FrequencyType = _cmbFrequency.SelectedItem?.ToString() ?? "Monthly";
            _scheduled.DayOfMonth = (int)_txtDayOfMonth.Value;
            _scheduled.NextExecutionDate = _dateNext.Value;
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
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
