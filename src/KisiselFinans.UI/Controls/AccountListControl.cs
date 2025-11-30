using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Forms;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

public class AccountListControl : UserControl
{
    private readonly int _userId;
    private DataGridView _grid = null!;
    private List<Account> _accounts = new();

    public AccountListControl(int userId)
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
            Text = "➕ Yeni Hesap Ekle",
            Location = new Point(0, 5),
            Size = new Size(150, 35)
        };
        AppTheme.StyleSuccessButton(btnAdd);
        btnAdd.Click += (s, e) => ShowAccountDialog(null);

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
            if (_grid.Columns[e.ColumnIndex].Name == "CurrentBalance")
            {
                var row = _grid.Rows[e.RowIndex].DataBoundItem as Account;
                if (row != null && row.CurrentBalance < 0)
                {
                    e.CellStyle!.ForeColor = AppTheme.AccentRed;
                }
            }
        };

        _grid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is Account row)
            {
                ShowAccountDialog(row.Id);
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
            var service = new AccountService(unitOfWork);

            _accounts = (await service.GetUserAccountsAsync(_userId)).ToList();

            var displayList = _accounts.Select(a => new
            {
                a.Id,
                a.AccountName,
                HesapTuru = a.AccountType?.TypeName ?? "-",
                a.CurrencyCode,
                BaslangicBakiye = a.InitialBalance,
                MevcutBakiye = a.CurrentBalance,
                KrediLimiti = a.CreditLimit,
                KesimGunu = a.CutoffDay,
                Aktif = a.IsActive ? "Evet" : "Hayır"
            }).ToList();

            _grid.DataSource = displayList;

            if (_grid.Columns.Count > 0)
            {
                _grid.Columns["Id"].Visible = false;
                _grid.Columns["AccountName"].HeaderText = "Hesap Adı";
                _grid.Columns["HesapTuru"].HeaderText = "Hesap Türü";
                _grid.Columns["CurrencyCode"].HeaderText = "Para Birimi";
                _grid.Columns["BaslangicBakiye"].HeaderText = "Başlangıç";
                _grid.Columns["BaslangicBakiye"].DefaultCellStyle.Format = "N2";
                _grid.Columns["MevcutBakiye"].HeaderText = "Mevcut Bakiye";
                _grid.Columns["MevcutBakiye"].DefaultCellStyle.Format = "N2";
                _grid.Columns["KrediLimiti"].HeaderText = "Kredi Limiti";
                _grid.Columns["KrediLimiti"].DefaultCellStyle.Format = "N2";
                _grid.Columns["KesimGunu"].HeaderText = "Kesim Günü";
                _grid.Columns["Aktif"].HeaderText = "Aktif";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
