using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

public class TransactionListControl : UserControl
{
    private readonly int _userId;
    private DataGridView _grid = null!;
    private DateTimePicker _dateFrom = null!;
    private DateTimePicker _dateTo = null!;
    private List<TransactionDto> _transactions = new();

    public TransactionListControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.PrimaryDark;
        Padding = new Padding(10);

        // Toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.PrimaryDark,
            Padding = new Padding(0, 5, 0, 10)
        };

        var lblFrom = new Label
        {
            Text = "Başlangıç:",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(0, 8),
            AutoSize = true
        };

        _dateFrom = new DateTimePicker
        {
            Location = new Point(70, 5),
            Size = new Size(130, 28),
            Format = DateTimePickerFormat.Short,
            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
        };

        var lblTo = new Label
        {
            Text = "Bitiş:",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(220, 8),
            AutoSize = true
        };

        _dateTo = new DateTimePicker
        {
            Location = new Point(260, 5),
            Size = new Size(130, 28),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now
        };

        var btnFilter = new Button
        {
            Text = "🔍 Filtrele",
            Location = new Point(410, 3),
            Size = new Size(100, 32)
        };
        AppTheme.StyleButton(btnFilter, true);
        btnFilter.Click += async (s, e) => await LoadDataAsync();

        var btnExport = new Button
        {
            Text = "📤 Excel",
            Location = new Point(520, 3),
            Size = new Size(90, 32)
        };
        AppTheme.StyleButton(btnExport);
        btnExport.Click += (s, e) => ExportToCsv();

        toolbar.Controls.AddRange(new Control[] { lblFrom, _dateFrom, lblTo, _dateTo, btnFilter, btnExport });

        // Grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        AppTheme.StyleDataGrid(_grid);

        _grid.CellFormatting += (s, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name == "TransactionTypeName")
            {
                var row = _grid.Rows[e.RowIndex].DataBoundItem as TransactionDto;
                if (row != null)
                {
                    e.CellStyle!.ForeColor = row.TransactionType switch
                    {
                        1 => AppTheme.AccentGreen,
                        2 => AppTheme.AccentRed,
                        _ => AppTheme.AccentBlue
                    };
                }
            }
        };

        _grid.CellDoubleClick += async (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is TransactionDto row)
            {
                if (MessageBox.Show($"'{row.Description}' işlemini silmek istiyor musunuz?", "Onay",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    await DeleteTransactionAsync(row.Id);
                }
            }
        };

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);

            _transactions = await service.GetTransactionsAsync(_userId, _dateFrom.Value.Date, _dateTo.Value.Date);
            _grid.DataSource = _transactions;

            // Kolon ayarları
            if (_grid.Columns.Count > 0)
            {
                _grid.Columns["Id"].Visible = false;
                _grid.Columns["AccountId"].Visible = false;
                _grid.Columns["CategoryId"].Visible = false;
                _grid.Columns["TransactionType"].Visible = false;
                _grid.Columns["CreatedAt"].Visible = false;

                _grid.Columns["TransactionDate"].HeaderText = "Tarih";
                _grid.Columns["TransactionDate"].DefaultCellStyle.Format = "dd.MM.yyyy";
                _grid.Columns["AccountName"].HeaderText = "Hesap";
                _grid.Columns["CategoryName"].HeaderText = "Kategori";
                _grid.Columns["Amount"].HeaderText = "Tutar";
                _grid.Columns["Amount"].DefaultCellStyle.Format = "N2";
                _grid.Columns["TransactionTypeName"].HeaderText = "Tür";
                _grid.Columns["Description"].HeaderText = "Açıklama";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteTransactionAsync(int id)
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);
            await service.DeleteTransactionAsync(id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportToCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV Files|*.csv",
            FileName = $"Islemler_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var lines = new List<string> { "Tarih;Hesap;Kategori;Tutar;Tür;Açıklama" };
            lines.AddRange(_transactions.Select(t =>
                $"{t.TransactionDate:dd.MM.yyyy};{t.AccountName};{t.CategoryName};{t.Amount:N2};{t.TransactionTypeName};{t.Description}"));

            File.WriteAllLines(dialog.FileName, lines, System.Text.Encoding.UTF8);
            MessageBox.Show("CSV dosyası oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}
