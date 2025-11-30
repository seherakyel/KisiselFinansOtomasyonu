using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.Utils;
using KisiselFinans.Core.Entities;
using KisiselFinans.UI.Controls;

namespace KisiselFinans.UI.Forms;

public partial class MainRibbonForm : RibbonForm
{
    private readonly User _currentUser;
    private XtraUserControl? _currentControl;

    public MainRibbonForm(User currentUser)
    {
        _currentUser = currentUser;
        InitializeComponent();
        LoadDashboard();
    }

    private void InitializeComponent()
    {
        Text = $"Kişisel Finans Otomasyonu - {_currentUser.FullName ?? _currentUser.Username}";
        Size = new Size(1400, 900);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        // Ribbon Control
        var ribbon = new RibbonControl { ShowApplicationButton = DefaultBoolean.False };
        Controls.Add(ribbon);

        // Ana Sayfa
        var pageHome = new RibbonPage("Ana Sayfa");
        ribbon.Pages.Add(pageHome);

        var groupDashboard = new RibbonPageGroup("Gösterge Paneli");
        pageHome.Groups.Add(groupDashboard);

        var btnDashboard = new BarButtonItem { Caption = "Dashboard", LargeGlyph = CreateIcon("📊", 32) };
        btnDashboard.ItemClick += (s, e) => LoadDashboard();
        groupDashboard.ItemLinks.Add(btnDashboard);

        // İşlemler Sayfası
        var pageTransactions = new RibbonPage("İşlemler");
        ribbon.Pages.Add(pageTransactions);

        var groupTransaction = new RibbonPageGroup("İşlem Yönetimi");
        pageTransactions.Groups.Add(groupTransaction);

        var btnNewIncome = new BarButtonItem { Caption = "Gelir Ekle", LargeGlyph = CreateIcon("💵", 32) };
        btnNewIncome.ItemClick += (s, e) => ShowTransactionDialog(1);
        groupTransaction.ItemLinks.Add(btnNewIncome);

        var btnNewExpense = new BarButtonItem { Caption = "Gider Ekle", LargeGlyph = CreateIcon("💸", 32) };
        btnNewExpense.ItemClick += (s, e) => ShowTransactionDialog(2);
        groupTransaction.ItemLinks.Add(btnNewExpense);

        var btnTransfer = new BarButtonItem { Caption = "Transfer", LargeGlyph = CreateIcon("🔄", 32) };
        btnTransfer.ItemClick += (s, e) => ShowTransferDialog();
        groupTransaction.ItemLinks.Add(btnTransfer);

        var btnAllTransactions = new BarButtonItem { Caption = "Tüm İşlemler", LargeGlyph = CreateIcon("📋", 32) };
        btnAllTransactions.ItemClick += (s, e) => LoadTransactionList();
        groupTransaction.ItemLinks.Add(btnAllTransactions);

        // Hesaplar Sayfası
        var pageAccounts = new RibbonPage("Hesaplar");
        ribbon.Pages.Add(pageAccounts);

        var groupAccounts = new RibbonPageGroup("Hesap Yönetimi");
        pageAccounts.Groups.Add(groupAccounts);

        var btnAccounts = new BarButtonItem { Caption = "Hesaplarım", LargeGlyph = CreateIcon("🏦", 32) };
        btnAccounts.ItemClick += (s, e) => LoadAccountList();
        groupAccounts.ItemLinks.Add(btnAccounts);

        var btnNewAccount = new BarButtonItem { Caption = "Yeni Hesap", LargeGlyph = CreateIcon("➕", 32) };
        btnNewAccount.ItemClick += (s, e) => ShowAccountDialog(null);
        groupAccounts.ItemLinks.Add(btnNewAccount);

        // Bütçe Sayfası
        var pageBudget = new RibbonPage("Bütçe");
        ribbon.Pages.Add(pageBudget);

        var groupBudget = new RibbonPageGroup("Bütçe Yönetimi");
        pageBudget.Groups.Add(groupBudget);

        var btnBudgets = new BarButtonItem { Caption = "Bütçeler", LargeGlyph = CreateIcon("📈", 32) };
        btnBudgets.ItemClick += (s, e) => LoadBudgetList();
        groupBudget.ItemLinks.Add(btnBudgets);

        var btnNewBudget = new BarButtonItem { Caption = "Yeni Bütçe", LargeGlyph = CreateIcon("🎯", 32) };
        btnNewBudget.ItemClick += (s, e) => ShowBudgetDialog(null);
        groupBudget.ItemLinks.Add(btnNewBudget);

        // Planlama Sayfası
        var pageSchedule = new RibbonPage("Planlama");
        ribbon.Pages.Add(pageSchedule);

        var groupSchedule = new RibbonPageGroup("Planlı İşlemler");
        pageSchedule.Groups.Add(groupSchedule);

        var btnScheduled = new BarButtonItem { Caption = "Planlı İşlemler", LargeGlyph = CreateIcon("📅", 32) };
        btnScheduled.ItemClick += (s, e) => LoadScheduledList();
        groupSchedule.ItemLinks.Add(btnScheduled);

        var btnCalendar = new BarButtonItem { Caption = "Takvim", LargeGlyph = CreateIcon("🗓️", 32) };
        btnCalendar.ItemClick += (s, e) => LoadCalendar();
        groupSchedule.ItemLinks.Add(btnCalendar);

        // Raporlar Sayfası
        var pageReports = new RibbonPage("Raporlar");
        ribbon.Pages.Add(pageReports);

        var groupReports = new RibbonPageGroup("Raporlama");
        pageReports.Groups.Add(groupReports);

        var btnMonthlyReport = new BarButtonItem { Caption = "Aylık Rapor", LargeGlyph = CreateIcon("📊", 32) };
        btnMonthlyReport.ItemClick += (s, e) => ShowMonthlyReport();
        groupReports.ItemLinks.Add(btnMonthlyReport);

        var btnCategoryReport = new BarButtonItem { Caption = "Kategori Raporu", LargeGlyph = CreateIcon("🥧", 32) };
        btnCategoryReport.ItemClick += (s, e) => ShowCategoryReport();
        groupReports.ItemLinks.Add(btnCategoryReport);

        // Kategoriler
        var groupCategories = new RibbonPageGroup("Kategoriler");
        pageReports.Groups.Add(groupCategories);

        var btnCategories = new BarButtonItem { Caption = "Kategoriler", LargeGlyph = CreateIcon("🏷️", 32) };
        btnCategories.ItemClick += (s, e) => LoadCategoryList();
        groupCategories.ItemLinks.Add(btnCategories);

        // Status Bar
        ribbon.StatusBar = new RibbonStatusBar();
        var statusUser = new BarStaticItem { Caption = $"Kullanıcı: {_currentUser.FullName ?? _currentUser.Username}" };
        ribbon.StatusBar.ItemLinks.Add(statusUser);
        var statusDate = new BarStaticItem { Caption = $"Tarih: {DateTime.Now:dd.MM.yyyy}" };
        ribbon.StatusBar.ItemLinks.Add(statusDate);

        // Panel for content
        var panelContent = new PanelControl
        {
            Name = "panelContent",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        Controls.Add(panelContent);
        panelContent.BringToFront();
    }

    private static Image CreateIcon(string emoji, int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        using var font = new Font("Segoe UI Emoji", size / 2);
        var textSize = g.MeasureString(emoji, font);
        g.DrawString(emoji, font, Brushes.Black, (size - textSize.Width) / 2, (size - textSize.Height) / 2);
        return bmp;
    }

    private void LoadControl(XtraUserControl control)
    {
        var panel = Controls["panelContent"] as PanelControl;
        if (panel == null) return;

        _currentControl?.Dispose();
        _currentControl = control;
        control.Dock = DockStyle.Fill;
        panel.Controls.Clear();
        panel.Controls.Add(control);
    }

    private void LoadDashboard() => LoadControl(new DashboardControl(_currentUser.Id));
    private void LoadTransactionList() => LoadControl(new TransactionListControl(_currentUser.Id));
    private void LoadAccountList() => LoadControl(new AccountListControl(_currentUser.Id));
    private void LoadBudgetList() => LoadControl(new BudgetListControl(_currentUser.Id));
    private void LoadScheduledList() => LoadControl(new ScheduledListControl(_currentUser.Id));
    private void LoadCalendar() => LoadControl(new CalendarControl(_currentUser.Id));
    private void LoadCategoryList() => LoadControl(new CategoryListControl(_currentUser.Id));

    private void ShowTransactionDialog(byte type)
    {
        using var dialog = new TransactionDialog(_currentUser.Id, type);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (_currentControl is DashboardControl dc) dc.RefreshData();
            else if (_currentControl is TransactionListControl tc) tc.RefreshData();
        }
    }

    private void ShowTransferDialog()
    {
        using var dialog = new TransferDialog(_currentUser.Id);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (_currentControl is DashboardControl dc) dc.RefreshData();
        }
    }

    private void ShowAccountDialog(int? accountId)
    {
        using var dialog = new AccountDialog(_currentUser.Id, accountId);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (_currentControl is AccountListControl ac) ac.RefreshData();
        }
    }

    private void ShowBudgetDialog(int? budgetId)
    {
        using var dialog = new BudgetDialog(_currentUser.Id, budgetId);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (_currentControl is BudgetListControl bc) bc.RefreshData();
        }
    }

    private void ShowMonthlyReport()
    {
        using var dialog = new ReportViewerForm(_currentUser.Id, "Monthly");
        dialog.ShowDialog();
    }

    private void ShowCategoryReport()
    {
        using var dialog = new ReportViewerForm(_currentUser.Id, "Category");
        dialog.ShowDialog();
    }
}

