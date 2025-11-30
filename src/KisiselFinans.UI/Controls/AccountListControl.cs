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

public class AccountListControl : XtraUserControl
{
    private readonly int _userId;
    private GridControl _grid = null!;
    private GridView _gridView = null!;
    private List<Account> _accounts = new();

    public AccountListControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var layout = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(layout);

        var btnAdd = new SimpleButton { Text = "Yeni Hesap Ekle", Size = new Size(150, 30) };
        btnAdd.Click += (s, e) => ShowAccountDialog(null);
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
            if (_gridView.FocusedRowHandle >= 0 && _gridView.GetRow(_gridView.FocusedRowHandle) is Account row)
            {
                ShowAccountDialog(row.Id);
            }
        };

        // Bakiye renklendirme
        _gridView.RowStyle += (s, e) =>
        {
            if (_gridView.GetRow(e.RowHandle) is Account row)
            {
                if (row.CurrentBalance < 0)
                    e.Appearance.ForeColor = Color.Red;
                else if (row.AccountTypeId == 3 && row.CurrentBalance > row.CreditLimit * 0.9m)
                    e.Appearance.ForeColor = Color.Orange;
            }
        };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new AccountService(unitOfWork);

            _accounts = (await service.GetUserAccountsAsync(_userId)).ToList();
            _grid.DataSource = _accounts;

            _gridView.PopulateColumns();
            _gridView.Columns["Id"].Visible = false;
            _gridView.Columns["UserId"].Visible = false;
            _gridView.Columns["AccountTypeId"].Visible = false;
            _gridView.Columns["User"].Visible = false;
            _gridView.Columns["Transactions"].Visible = false;
            _gridView.Columns["ScheduledTransactions"].Visible = false;

            _gridView.Columns["AccountName"].Caption = "Hesap Adı";
            _gridView.Columns["AccountType"].Caption = "Hesap Türü";
            _gridView.Columns["CurrencyCode"].Caption = "Para Birimi";
            _gridView.Columns["InitialBalance"].Caption = "Başlangıç Bakiyesi";
            _gridView.Columns["InitialBalance"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["CurrentBalance"].Caption = "Mevcut Bakiye";
            _gridView.Columns["CurrentBalance"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["CreditLimit"].Caption = "Kredi Limiti";
            _gridView.Columns["CreditLimit"].DisplayFormat.FormatString = "N2";
            _gridView.Columns["CutoffDay"].Caption = "Hesap Kesim Günü";
            _gridView.Columns["IsActive"].Caption = "Aktif";

            _gridView.BestFitColumns();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAccountDialog(int? accountId)
    {
        using var dialog = new AccountDialog(_userId, accountId);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _ = LoadDataAsync();
        }
    }

    public void RefreshData() => _ = LoadDataAsync();
}

