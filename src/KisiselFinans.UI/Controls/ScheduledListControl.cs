using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Forms;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

public class ScheduledListControl : UserControl
{
    private readonly int _userId;
    private DataGridView _grid = null!;

    public ScheduledListControl(int userId)
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

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.PrimaryDark
        };

        var btnAdd = new Button
        {
            Text = "➕ Yeni Planlı İşlem",
            Location = new Point(0, 5),
            Size = new Size(160, 35)
        };
        AppTheme.StyleSuccessButton(btnAdd);
        btnAdd.Click += (s, e) =>
        {
            using var dialog = new ScheduledTransactionDialog(_userId, null);
            if (dialog.ShowDialog() == DialogResult.OK)
                _ = LoadDataAsync();
        };

        var btnExecute = new Button
        {
            Text = "▶️ Vadesi Gelenleri Uygula",
            Location = new Point(170, 5),
            Size = new Size(180, 35)
        };
        AppTheme.StyleButton(btnExecute, true);
        btnExecute.Click += async (s, e) => await ExecuteScheduledAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnExecute });

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
            if (_grid.Columns[e.ColumnIndex].Name == "SonrakiTarih")
            {
                var dateValue = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                if (dateValue != null)
                {
                    var date = Convert.ToDateTime(dateValue);
                    var daysLeft = (date - DateTime.Now).Days;
                    if (daysLeft <= 0)
                        e.CellStyle!.BackColor = Color.FromArgb(80, 244, 67, 54);
                    else if (daysLeft <= 3)
                        e.CellStyle!.BackColor = Color.FromArgb(80, 255, 152, 0);
                }
            }
        };

        _grid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                var id = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Id"].Value);
                using var dialog = new ScheduledTransactionDialog(_userId, id);
                if (dialog.ShowDialog() == DialogResult.OK)
                    _ = LoadDataAsync();
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
            var service = new ScheduledTransactionService(unitOfWork);

            var scheduled = (await service.GetUserScheduledTransactionsAsync(_userId)).ToList();

            var displayList = scheduled.Select(s => new
            {
                s.Id,
                Hesap = s.Account?.AccountName ?? "-",
                Kategori = s.Category?.CategoryName ?? "-",
                Tutar = s.Amount,
                Aciklama = s.Description,
                Siklik = s.FrequencyType,
                Gun = s.DayOfMonth,
                SonrakiTarih = s.NextExecutionDate,
                Aktif = s.IsActive ? "Evet" : "Hayır"
            }).ToList();

            _grid.DataSource = displayList;

            if (_grid.Columns.Count > 0)
            {
                _grid.Columns["Id"].Visible = false;
                _grid.Columns["Hesap"].HeaderText = "Hesap";
                _grid.Columns["Kategori"].HeaderText = "Kategori";
                _grid.Columns["Tutar"].HeaderText = "Tutar";
                _grid.Columns["Tutar"].DefaultCellStyle.Format = "N2";
                _grid.Columns["Aciklama"].HeaderText = "Açıklama";
                _grid.Columns["Siklik"].HeaderText = "Sıklık";
                _grid.Columns["Gun"].HeaderText = "Gün";
                _grid.Columns["SonrakiTarih"].HeaderText = "Sonraki Tarih";
                _grid.Columns["SonrakiTarih"].DefaultCellStyle.Format = "dd.MM.yyyy";
                _grid.Columns["Aktif"].HeaderText = "Aktif";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ExecuteScheduledAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new ScheduledTransactionService(unitOfWork);

            await service.ExecuteScheduledTransactionsAsync();
            MessageBox.Show("Planlı işlemler uygulandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}
