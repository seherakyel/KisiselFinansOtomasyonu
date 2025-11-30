using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Forms;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

public class CategoryListControl : UserControl
{
    private readonly int _userId;
    private DataGridView _grid = null!;

    public CategoryListControl(int userId)
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
            Text = "➕ Yeni Kategori",
            Location = new Point(0, 5),
            Size = new Size(140, 35)
        };
        AppTheme.StyleSuccessButton(btnAdd);
        btnAdd.Click += (s, e) =>
        {
            using var dialog = new CategoryDialog(_userId, null);
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
            if (_grid.Columns[e.ColumnIndex].Name == "Tur")
            {
                var value = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                e.CellStyle!.ForeColor = value == "Gelir" ? AppTheme.AccentGreen : AppTheme.AccentRed;
            }
        };

        _grid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                var id = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Id"].Value);
                using var dialog = new CategoryDialog(_userId, id);
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
            var service = new CategoryService(unitOfWork);

            var categories = (await service.GetUserCategoriesAsync(_userId)).ToList();

            var displayList = categories.Select(c => new
            {
                c.Id,
                KategoriAdi = c.CategoryName,
                Tur = c.Type == 1 ? "Gelir" : "Gider",
                Ikon = c.IconIndex
            }).ToList();

            _grid.DataSource = displayList;

            if (_grid.Columns.Count > 0)
            {
                _grid.Columns["Id"].Visible = false;
                _grid.Columns["KategoriAdi"].HeaderText = "Kategori Adı";
                _grid.Columns["Tur"].HeaderText = "Tür";
                _grid.Columns["Ikon"].HeaderText = "İkon";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}
