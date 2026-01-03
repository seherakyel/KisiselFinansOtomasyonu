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
    private TextBox _txtAmount = null!;
    private TextBox _txtDescription = null!;
    private ComboBox _cmbFrequency = null!;
    private NumericUpDown _numDayOfMonth = null!;
    private DateTimePicker _dateNext = null!;
    private CheckBox _chkActive = null!;

    private const int DIALOG_WIDTH = 440;
    private const int DIALOG_HEIGHT = 600;
    private const int FIELD_WIDTH = 392;

    private static readonly Color AccentCyan = Color.FromArgb(6, 182, 212);

    public ScheduledTransactionDialog(int userId, int? scheduledId = null)
    {
        _userId = userId;
        _scheduledId = scheduledId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _scheduledId.HasValue;
        DialogStyles.ApplyDialogStyle(this, DIALOG_WIDTH, DIALOG_HEIGHT);

        // Header
        var header = DialogStyles.CreateHeader(
            "📅", isEdit ? "Planlı İşlem Düzenle" : "Yeni Planlı İşlem", "Otomatik tekrarlayan işlem",
            AccentCyan,
            () => { DialogResult = DialogResult.Cancel; Close(); });

        // Content
        var content = DialogStyles.CreateContentPanel();

        int y = 8;
        int spacing = 64;
        int halfWidth = (FIELD_WIDTH - 12) / 2;

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

        // Tutar - Sıklık (yan yana)
        content.Controls.Add(DialogStyles.CreateLabel("Tutar", 0, y));
        var (amountContainer, amountTxt) = DialogStyles.CreateCurrencyInput(0, y + 24, halfWidth, AccentCyan);
        _txtAmount = amountTxt;
        content.Controls.Add(amountContainer);

        content.Controls.Add(DialogStyles.CreateLabel("Tekrar Sıklığı", halfWidth + 12, y));
        _cmbFrequency = DialogStyles.CreateComboBox(halfWidth + 12, y + 24, halfWidth);
        _cmbFrequency.Items.AddRange(new[] { "Günlük", "Haftalık", "Aylık", "Yıllık" });
        _cmbFrequency.SelectedIndex = 2;
        content.Controls.Add(_cmbFrequency);
        y += spacing + 8;

        // Gün - Sonraki Tarih (yan yana)
        content.Controls.Add(DialogStyles.CreateLabel("Ayın Günü", 0, y));
        _numDayOfMonth = new NumericUpDown
        {
            Location = new Point(0, y + 24),
            Size = new Size(80, 36),
            Minimum = 1,
            Maximum = 31,
            Value = DateTime.Now.Day,
            Font = new Font("Segoe UI", 11),
            BackColor = DialogStyles.BgInput,
            ForeColor = DialogStyles.TextWhite,
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(_numDayOfMonth);

        content.Controls.Add(DialogStyles.CreateLabel("Sonraki İşlem Tarihi", 100, y));
        _dateNext = DialogStyles.CreateDatePicker(100, y + 24, FIELD_WIDTH - 100);
        _dateNext.Value = DateTime.Now.AddMonths(1);
        content.Controls.Add(_dateNext);
        y += spacing;

        // Açıklama
        content.Controls.Add(DialogStyles.CreateLabel("Açıklama (Opsiyonel)", 0, y));
        _txtDescription = DialogStyles.CreateTextBox(0, y + 24, FIELD_WIDTH);
        content.Controls.Add(_txtDescription);
        y += spacing;

        // Aktif checkbox
        var activeContainer = new Panel
        {
            Location = new Point(0, y),
            Size = new Size(FIELD_WIDTH, 40),
            BackColor = DialogStyles.BgInput
        };
        activeContainer.Paint += (s, e) =>
        {
            using var pen = new Pen(DialogStyles.BorderDefault, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, activeContainer.Width - 1, activeContainer.Height - 1);
        };

        _chkActive = new CheckBox
        {
            Text = "  Bu planlı işlem aktif",
            Font = new Font("Segoe UI Semibold", 10),
            ForeColor = DialogStyles.TextWhite,
            Location = new Point(12, 10),
            AutoSize = true,
            Checked = true,
            BackColor = Color.Transparent
        };
        activeContainer.Controls.Add(_chkActive);
        content.Controls.Add(activeContainer);

        // Footer
        var footer = DialogStyles.CreateFooter(
            isEdit ? "Güncelle" : "Planla",
            AccentCyan,
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

            var accountService = new AccountService(unitOfWork);
            var categoryService = new CategoryService(unitOfWork);
            var scheduledService = new ScheduledTransactionService(unitOfWork);

            var accounts = (await accountService.GetUserAccountsAsync(_userId)).ToList();
            var categories = (await categoryService.GetUserCategoriesAsync(_userId)).ToList();

            BeginInvoke(() =>
            {
                _cmbAccount.DataSource = accounts;
                _cmbAccount.DisplayMember = "AccountName";
                _cmbAccount.ValueMember = "Id";

                _cmbCategory.DataSource = categories;
                _cmbCategory.DisplayMember = "CategoryName";
                _cmbCategory.ValueMember = "Id";
            });

            if (_scheduledId.HasValue)
            {
                _scheduled = await scheduledService.GetByIdAsync(_scheduledId.Value);
                if (_scheduled != null)
                {
                    BeginInvoke(() =>
                    {
                        _cmbAccount.SelectedValue = _scheduled.AccountId;
                        _cmbCategory.SelectedValue = _scheduled.CategoryId;
                        _txtAmount.Text = _scheduled.Amount.ToString("N2");
                        _cmbFrequency.SelectedIndex = _scheduled.FrequencyType switch
                        {
                            "Daily" => 0,
                            "Weekly" => 1,
                            "Yearly" => 3,
                            _ => 2
                        };
                        _numDayOfMonth.Value = _scheduled.DayOfMonth ?? 1;
                        _dateNext.Value = _scheduled.NextExecutionDate;
                        _txtDescription.Text = _scheduled.Description ?? "";
                        _chkActive.Checked = _scheduled.IsActive;
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
        if (_cmbAccount.SelectedValue == null || _cmbCategory.SelectedValue == null)
        {
            MessageBox.Show("Lütfen hesap ve kategori seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(_txtAmount.Text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount) || amount <= 0)
        {
            MessageBox.Show("Lütfen geçerli bir tutar giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new ScheduledTransactionService(unitOfWork);

            if (_scheduledId.HasValue)
                _scheduled = await service.GetByIdAsync(_scheduledId.Value);
            else
                _scheduled = new ScheduledTransaction { UserId = _userId };

            _scheduled!.AccountId = (int)_cmbAccount.SelectedValue;
            _scheduled.CategoryId = (int)_cmbCategory.SelectedValue;
            _scheduled.Amount = amount;
            _scheduled.FrequencyType = _cmbFrequency.SelectedIndex switch
            {
                0 => "Daily",
                1 => "Weekly",
                3 => "Yearly",
                _ => "Monthly"
            };
            _scheduled.DayOfMonth = (int)_numDayOfMonth.Value;
            _scheduled.NextExecutionDate = _dateNext.Value.Date;
            _scheduled.Description = _txtDescription.Text.Trim();
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
