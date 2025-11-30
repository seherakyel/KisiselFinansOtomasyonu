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

public class ScheduledListControl : XtraUserControl
{
    private readonly int _userId;
    private GridControl _grid = null!;
    private GridView _gridView = null!;

    public ScheduledListControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var layout = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(layout);

        var btnAdd = new SimpleButton { Text = "Yeni Planlı İşlem", Size = new Size(150, 30) };
        btnAdd.Click += (s, e) =>
        {
            using var dialog = new ScheduledTransactionDialog(_userId, null);
            if (dialog.ShowDialog() == DialogResult.OK)
                _ = LoadDataAsync();
        };
        layout.Root.AddItem(new LayoutControlItem { Control = btnAdd, TextVisible = false });

        var btnExecute = new SimpleButton { Text = "Vadesi Gelenleri Uygula", Size = new Size(180, 30) };
        btnExecute.Click += async (s, e) => await ExecuteScheduledAsync();
        layout.Root.AddItem(new LayoutControlItem { Control = btnExecute, TextVisible = false });

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
            if (_gridView.FocusedRowHandle >= 0 && _gridView.GetRow(_gridView.FocusedRowHandle) is ScheduledTransaction row)
            {
                using var dialog = new ScheduledTransactionDialog(_userId, row.Id);
                if (dialog.ShowDialog() == DialogResult.OK)
                    _ = LoadDataAsync();
            }
        };

        // Yaklaşan işlemler için renklendirme
        _gridView.RowStyle += (s, e) =>
        {
            if (_gridView.GetRow(e.RowHandle) is ScheduledTransaction row)
            {
                var daysLeft = (row.NextExecutionDate - DateTime.Now).Days;
                if (daysLeft <= 0)
                    e.Appearance.BackColor = Color.FromArgb(255, 200, 200);
                else if (daysLeft <= 3)
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
            var service = new ScheduledTransactionService(unitOfWork);

            var scheduled = (await service.GetUserScheduledTransactionsAsync(_userId)).ToList();
            _grid.DataSource = scheduled;

            _gridView.PopulateColumns();
            _gridView.Columns["Id"].Visible = false;
            _gridView.Columns["UserId"].Visible = false;
            _gridView.Columns["AccountId"].Visible = false;
            _gridView.Columns["CategoryId"].Visible = false;
            _gridView.Columns["User"].Visible = false;

            _gridView.Columns["Account"].Caption = "Hesap";
            _gridView.Columns["Category"].Caption = "Kategori";
            _gridView.Columns["Amount"].Caption = "Tutar";
            _gridView.Columns["Amount"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["Description"].Caption = "Açıklama";
            _gridView.Columns["FrequencyType"].Caption = "Sıklık";
            _gridView.Columns["DayOfMonth"].Caption = "Gün";
            _gridView.Columns["NextExecutionDate"].Caption = "Sonraki Tarih";
            _gridView.Columns["NextExecutionDate"].DisplayFormat.FormatString = "dd.MM.yyyy";
            _gridView.Columns["IsActive"].Caption = "Aktif";

            _gridView.BestFitColumns();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            XtraMessageBox.Show("Planlı işlemler uygulandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}

