using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Forms;

namespace KisiselFinans.UI.Controls;

public class CategoryListControl : XtraUserControl
{
    private readonly int _userId;
    private GridControl _grid = null!;
    private GridView _gridView = null!;

    public CategoryListControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var layout = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(layout);

        var btnAdd = new SimpleButton { Text = "Yeni Kategori", Size = new Size(150, 30) };
        btnAdd.Click += (s, e) =>
        {
            using var dialog = new CategoryDialog(_userId, null);
            if (dialog.ShowDialog() == DialogResult.OK)
                _ = LoadDataAsync();
        };
        layout.Root.AddItem(new LayoutControlItem { Control = btnAdd, TextVisible = false });

        _grid = new GridControl();
        _gridView = new GridView(_grid);
        _grid.MainView = _gridView;

        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsView.ShowGroupPanel = false;

        var gridItem = new LayoutControlItem { Control = _grid, TextVisible = false };
        gridItem.SizeConstraintsType = SizeConstraintsType.Custom;
        gridItem.MinSize = new Size(800, 500);
        layout.Root.AddItem(gridItem);

        _gridView.DoubleClick += (s, e) =>
        {
            if (_gridView.FocusedRowHandle >= 0 && _gridView.GetRow(_gridView.FocusedRowHandle) is Category row)
            {
                using var dialog = new CategoryDialog(_userId, row.Id);
                if (dialog.ShowDialog() == DialogResult.OK)
                    _ = LoadDataAsync();
            }
        };

        _gridView.RowStyle += (s, e) =>
        {
            if (_gridView.GetRow(e.RowHandle) is Category row)
            {
                e.Appearance.ForeColor = row.Type == 1 ? Color.Green : Color.Red;
            }
        };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new CategoryService(unitOfWork);

            var categories = (await service.GetUserCategoriesAsync(_userId)).ToList();
            _grid.DataSource = categories;

            _gridView.PopulateColumns();
            _gridView.Columns["Id"].Visible = false;
            _gridView.Columns["UserId"].Visible = false;
            _gridView.Columns["ParentId"].Visible = false;
            _gridView.Columns["User"].Visible = false;
            _gridView.Columns["ParentCategory"].Visible = false;
            _gridView.Columns["SubCategories"].Visible = false;
            _gridView.Columns["Transactions"].Visible = false;
            _gridView.Columns["Budgets"].Visible = false;
            _gridView.Columns["ScheduledTransactions"].Visible = false;

            _gridView.Columns["CategoryName"].Caption = "Kategori Adı";
            _gridView.Columns["Type"].Caption = "Tür";
            _gridView.Columns["IconIndex"].Caption = "İkon";

            _gridView.BestFitColumns();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}

