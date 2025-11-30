using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Forms;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

public class BudgetListControl : UserControl
{
    private readonly int _userId;
    private DataGridView _grid = null!;

    public BudgetListControl(int userId)
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
            Text = "➕ Yeni Bütçe Ekle",
            Location = new Point(0, 5),
            Size = new Size(150, 35)
        };
        AppTheme.StyleSuccessButton(btnAdd);
        btnAdd.Click += (s, e) =>
        {
            using var dialog = new BudgetDialog(_userId, null);
            if (dialog.ShowDialog() == DialogResult.OK)
                _ = LoadDataAsync();
        };

        toolbar.Controls.Add(btnAdd);

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
            if (_grid.Columns[e.ColumnIndex].Name == "Yuzde")
            {
                var value = Convert.ToDouble(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                if (value > 90)
                    e.CellStyle!.BackColor = Color.FromArgb(80, 244, 67, 54);
                else if (value > 70)
                    e.CellStyle!.BackColor = Color.FromArgb(80, 255, 152, 0);
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
            var service = new BudgetService(unitOfWork);

            var budgets = await service.GetBudgetStatusesAsync(_userId);

            var displayList = budgets.Select(b => new
            {
                Kategori = b.CategoryName,
                Limit = b.Limit,
                Harcanan = b.Spent,
                Kalan = b.Remaining,
                Yuzde = b.Percentage,
                Durum = b.IsOverBudget ? "❌ Aşıldı" : "✅ Normal"
            }).ToList();

            _grid.DataSource = displayList;

            if (_grid.Columns.Count > 0)
            {
                _grid.Columns["Kategori"].HeaderText = "Kategori";
                _grid.Columns["Limit"].HeaderText = "Limit";
                _grid.Columns["Limit"].DefaultCellStyle.Format = "N2";
                _grid.Columns["Harcanan"].HeaderText = "Harcanan";
                _grid.Columns["Harcanan"].DefaultCellStyle.Format = "N2";
                _grid.Columns["Kalan"].HeaderText = "Kalan";
                _grid.Columns["Kalan"].DefaultCellStyle.Format = "N2";
                _grid.Columns["Yuzde"].HeaderText = "Yüzde %";
                _grid.Columns["Yuzde"].DefaultCellStyle.Format = "F1";
                _grid.Columns["Durum"].HeaderText = "Durum";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}
