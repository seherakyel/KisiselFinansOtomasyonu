using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;
using KisiselFinans.UI.Controls;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

/// <summary>
/// CSV Banka Ekstresi Import Diyaloğu ⭐
/// </summary>
public class CsvImportDialog : Form
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CsvImportService _importService;
    private readonly int _userId;

    private ComboBox _cmbAccount = null!;
    private DataGridView _gridPreview = null!;
    private Label _lblStatus = null!;
    private Button _btnImport = null!;
    private CsvImportResultDto? _importResult;

    public CsvImportDialog(IUnitOfWork unitOfWork, int userId)
    {
        _unitOfWork = unitOfWork;
        _importService = new CsvImportService(unitOfWork);
        _userId = userId;
        InitializeComponents();
        LoadAccounts();
    }

    private void InitializeComponents()
    {
        Text = "CSV Banka Ekstresi Import";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.PrimaryDark;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Header
        var header = DialogStyles.CreateHeaderPanel(
            "CSV Import",
            "Banka ekstrenizi içe aktarın",
            "📥",
            AppTheme.AccentBlue,
            () => { DialogResult = DialogResult.Cancel; Close(); }
        );
        Controls.Add(header);

        // Content Panel
        var content = new Panel
        {
            Location = new Point(0, 80),
            Size = new Size(Width, Height - 80),
            BackColor = AppTheme.PrimaryDark,
            Padding = new Padding(20)
        };
        Controls.Add(content);

        // Dosya seçim paneli
        var filePanel = new Panel
        {
            Location = new Point(20, 10),
            Size = new Size(740, 50),
            BackColor = AppTheme.CardBackground
        };

        var lblFile = new Label
        {
            Text = "CSV Dosyası:",
            Font = new Font("Segoe UI", 10),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(10, 15),
            AutoSize = true
        };
        filePanel.Controls.Add(lblFile);

        var txtFilePath = new TextBox
        {
            Location = new Point(100, 12),
            Size = new Size(480, 30),
            BackColor = Color.FromArgb(55, 65, 81),
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true
        };
        filePanel.Controls.Add(txtFilePath);

        var btnBrowse = new Button
        {
            Text = "📂 Dosya Seç",
            Location = new Point(590, 10),
            Size = new Size(130, 32),
            BackColor = AppTheme.AccentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnBrowse.FlatAppearance.BorderSize = 0;
        btnBrowse.Click += async (s, e) =>
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "CSV Dosyaları|*.csv|Tüm Dosyalar|*.*",
                Title = "Banka Ekstresi Seçin"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
                await LoadPreviewAsync(ofd.FileName);
            }
        };
        filePanel.Controls.Add(btnBrowse);

        content.Controls.Add(filePanel);

        // Hesap seçimi
        var lblAccount = new Label
        {
            Text = "Hedef Hesap:",
            Font = new Font("Segoe UI", 10),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(20, 70),
            AutoSize = true
        };
        content.Controls.Add(lblAccount);

        _cmbAccount = new ComboBox
        {
            Location = new Point(120, 67),
            Size = new Size(300, 30),
            BackColor = Color.FromArgb(55, 65, 81),
            ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        content.Controls.Add(_cmbAccount);

        // Preview Grid
        _gridPreview = new DataGridView
        {
            Location = new Point(20, 110),
            Size = new Size(740, 300),
            BackgroundColor = AppTheme.CardBackground,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            GridColor = Color.FromArgb(55, 65, 81),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        // Grid styling
        _gridPreview.DefaultCellStyle.BackColor = AppTheme.CardBackground;
        _gridPreview.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _gridPreview.DefaultCellStyle.SelectionBackColor = AppTheme.AccentBlue;
        _gridPreview.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 46, 54);
        _gridPreview.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _gridPreview.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9);
        _gridPreview.EnableHeadersVisualStyles = false;

        content.Controls.Add(_gridPreview);

        // Status label
        _lblStatus = new Label
        {
            Text = "Bir CSV dosyası seçin",
            Font = new Font("Segoe UI", 9),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(20, 420),
            AutoSize = true
        };
        content.Controls.Add(_lblStatus);

        // Butonlar
        var btnCancel = new Button
        {
            Text = "İptal",
            Location = new Point(540, 450),
            Size = new Size(100, 38),
            BackColor = Color.FromArgb(55, 65, 81),
            ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        content.Controls.Add(btnCancel);

        _btnImport = new Button
        {
            Text = "📥 İçe Aktar",
            Location = new Point(650, 450),
            Size = new Size(120, 38),
            BackColor = AppTheme.AccentGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = false
        };
        _btnImport.FlatAppearance.BorderSize = 0;
        _btnImport.Click += async (s, e) => await ImportTransactionsAsync();
        content.Controls.Add(_btnImport);
    }

    private async void LoadAccounts()
    {
        var accounts = await _unitOfWork.Accounts.FindAsync(a => a.UserId == _userId && a.IsActive);
        _cmbAccount.DataSource = accounts.ToList();
        _cmbAccount.DisplayMember = "AccountName";
        _cmbAccount.ValueMember = "Id";
    }

    private async Task LoadPreviewAsync(string filePath)
    {
        try
        {
            _lblStatus.Text = "Dosya okunuyor...";
            _lblStatus.ForeColor = AppTheme.TextSecondary;

            _importResult = await _importService.ParseCsvAsync(filePath, _userId);

            _gridPreview.DataSource = _importResult.ImportedTransactions
                .Select(t => new
                {
                    Tarih = t.Date.ToString("dd.MM.yyyy"),
                    Açıklama = t.Description,
                    Tutar = $"{(t.IsIncome ? "+" : "-")}₺{t.Amount:N2}",
                    Tür = t.IsIncome ? "Gelir" : "Gider",
                    Kategori = t.SuggestedCategory
                })
                .ToList();

            _gridPreview.Columns[0].Width = 80;
            _gridPreview.Columns[1].Width = 300;
            _gridPreview.Columns[2].Width = 100;
            _gridPreview.Columns[3].Width = 70;
            _gridPreview.Columns[4].Width = 130;

            _lblStatus.Text = $"✓ {_importResult.SuccessCount} işlem bulundu. Toplam: ₺{_importResult.TotalImportedAmount:N2}";
            _lblStatus.ForeColor = AppTheme.AccentGreen;
            _btnImport.Enabled = _importResult.SuccessCount > 0;

            if (_importResult.Errors.Any())
            {
                _lblStatus.Text += $" | ⚠ {_importResult.FailedCount} hata";
                _lblStatus.ForeColor = Color.FromArgb(255, 166, 87);
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Hata: {ex.Message}";
            _lblStatus.ForeColor = Color.FromArgb(248, 81, 73);
            _btnImport.Enabled = false;
        }
    }

    private async Task ImportTransactionsAsync()
    {
        if (_importResult == null || _cmbAccount.SelectedValue == null) return;

        try
        {
            _btnImport.Enabled = false;
            _btnImport.Text = "İçe aktarılıyor...";

            var accountId = (int)_cmbAccount.SelectedValue;
            var imported = await _importService.ImportTransactionsAsync(_userId, accountId, _importResult.ImportedTransactions);

            Toast.Success("İçe Aktarma Tamamlandı!", $"{imported} işlem başarıyla eklendi.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Toast.Error("Hata!", ex.Message);
            _btnImport.Enabled = true;
            _btnImport.Text = "📥 İçe Aktar";
        }
    }
}

