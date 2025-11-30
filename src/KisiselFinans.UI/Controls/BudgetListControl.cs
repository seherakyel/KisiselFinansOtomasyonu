using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Forms;

namespace KisiselFinans.UI.Controls;

public class BudgetListControl : XtraUserControl
{
    private readonly int _userId;
    private GridControl _grid = null!;
    private GridView _gridView = null!;

    public BudgetListControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var layout = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(layout);

        var btnAdd = new SimpleButton { Text = "Yeni Bütçe Ekle", Size = new Size(150, 30) };
        btnAdd.Click += (s, e) =>
        {
            using var dialog = new BudgetDialog(_userId, null);
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

        // Bütçe durumuna göre renklendirme
        _gridView.RowStyle += (s, e) =>
        {
            if (_gridView.GetRow(e.RowHandle) is BudgetStatusDto row)
            {
                if (row.IsOverBudget)
                    e.Appearance.BackColor = Color.FromArgb(255, 200, 200);
                else if (row.Percentage > 80)
                    e.Appearance.BackColor = Color.FromArgb(255, 240, 200);
            }
        };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new BudgetService(unitOfWork);

            var budgets = await service.GetBudgetStatusesAsync(_userId);
            _grid.DataSource = budgets;

            _gridView.PopulateColumns();
            _gridView.Columns["CategoryName"].Caption = "Kategori";
            _gridView.Columns["Limit"].Caption = "Limit";
            _gridView.Columns["Limit"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["Spent"].Caption = "Harcanan";
            _gridView.Columns["Spent"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["Remaining"].Caption = "Kalan";
            _gridView.Columns["Remaining"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["Percentage"].Caption = "Yüzde";
            _gridView.Columns["Percentage"].DisplayFormat.FormatString = "F1";
            _gridView.Columns["IsOverBudget"].Caption = "Aşıldı mı?";

            _gridView.BestFitColumns();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}

