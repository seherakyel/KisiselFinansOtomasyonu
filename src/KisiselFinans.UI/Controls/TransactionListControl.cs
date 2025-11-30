using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Controls;

public class TransactionListControl : XtraUserControl
{
    private readonly int _userId;
    private GridControl _grid = null!;
    private GridView _gridView = null!;
    private DateEdit _dateFrom = null!;
    private DateEdit _dateTo = null!;
    private List<TransactionDto> _transactions = new();

    public TransactionListControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var layout = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(layout);

        // Filtre alanları
        _dateFrom = new DateEdit { EditValue = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };
        _dateTo = new DateEdit { EditValue = DateTime.Now };

        var btnFilter = new SimpleButton { Text = "Filtrele" };
        btnFilter.Click += async (s, e) => await LoadDataAsync();

        var btnExport = new SimpleButton { Text = "Excel'e Aktar" };
        btnExport.Click += (s, e) => ExportToExcel();

        layout.Root.AddItem(new LayoutControlItem { Control = _dateFrom, Text = "Başlangıç" });
        layout.Root.AddItem(new LayoutControlItem { Control = _dateTo, Text = "Bitiş" });
        layout.Root.AddItem(new LayoutControlItem { Control = btnFilter, TextVisible = false });
        layout.Root.AddItem(new LayoutControlItem { Control = btnExport, TextVisible = false });

        // Grid
        _grid = new GridControl();
        _gridView = new GridView(_grid);
        _grid.MainView = _gridView;
        _grid.Dock = DockStyle.None;

        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.RowAutoHeight = true;

        var gridItem = new LayoutControlItem { Control = _grid, TextVisible = false };
        gridItem.SizeConstraintsType = SizeConstraintsType.Custom;
        gridItem.MinSize = new Size(800, 500);
        layout.Root.AddItem(gridItem);

        // Satır renklendirme
        _gridView.RowStyle += (s, e) =>
        {
            if (_gridView.GetRow(e.RowHandle) is TransactionDto row)
            {
                e.Appearance.ForeColor = row.TransactionType switch
                {
                    1 => Color.Green,
                    2 => Color.Red,
                    _ => Color.Blue
                };
            }
        };

        // Çift tıklama ile silme
        _gridView.DoubleClick += async (s, e) =>
        {
            if (_gridView.FocusedRowHandle >= 0 && _gridView.GetRow(_gridView.FocusedRowHandle) is TransactionDto row)
            {
                if (XtraMessageBox.Show($"'{row.Description}' işlemini silmek istiyor musunuz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    await DeleteTransactionAsync(row.Id);
                }
            }
        };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new TransactionService(unitOfWork);

            _transactions = await service.GetTransactionsAsync(_userId, (DateTime?)_dateFrom.EditValue, (DateTime?)_dateTo.EditValue);
            _grid.DataSource = _transactions;

            // Kolonları ayarla
            _gridView.PopulateColumns();
            _gridView.Columns["Id"].Visible = false;
            _gridView.Columns["AccountId"].Visible = false;
            _gridView.Columns["CategoryId"].Visible = false;
            _gridView.Columns["TransactionType"].Visible = false;
            _gridView.Columns["CreatedAt"].Visible = false;

            _gridView.Columns["TransactionDate"].Caption = "Tarih";
            _gridView.Columns["TransactionDate"].DisplayFormat.FormatString = "dd.MM.yyyy";
            _gridView.Columns["AccountName"].Caption = "Hesap";
            _gridView.Columns["CategoryName"].Caption = "Kategori";
            _gridView.Columns["Amount"].Caption = "Tutar";
            _gridView.Columns["Amount"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["TransactionTypeName"].Caption = "Tür";
            _gridView.Columns["Description"].Caption = "Açıklama";

            _gridView.BestFitColumns();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            XtraMessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportToExcel()
    {
        using var dialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"Islemler_{DateTime.Now:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _gridView.ExportToXlsx(dialog.FileName);
            XtraMessageBox.Show("Excel dosyası oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}

