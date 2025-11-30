using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraGauges.Win;
using DevExpress.XtraGauges.Win.Gauges.Circular;
using DevExpress.XtraGauges.Win.Gauges.Linear;
using DevExpress.XtraLayout;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Controls;

public class DashboardControl : XtraUserControl
{
    private readonly int _userId;
    private DashboardSummaryDto? _summary;
    private ForecastDto? _forecast;
    private LayoutControl _layoutControl = null!;

    public DashboardControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        _layoutControl = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(_layoutControl);

        var lblLoading = new LabelControl
        {
            Text = "Veriler yükleniyor...",
            Dock = DockStyle.Fill,
            Appearance = { TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center, VAlignment = DevExpress.Utils.VertAlignment.Center } }
        };
        Controls.Add(lblLoading);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var accountService = new AccountService(unitOfWork);
            var budgetService = new BudgetService(unitOfWork);
            var scheduledService = new ScheduledTransactionService(unitOfWork);
            var dashboardService = new DashboardService(unitOfWork, accountService, budgetService, scheduledService);

            _summary = await dashboardService.GetDashboardSummaryAsync(_userId);
            _forecast = await dashboardService.GetForecastAsync(_userId);

            BeginInvoke(() => BuildDashboard());
        }
        catch (Exception ex)
        {
            BeginInvoke(() => XtraMessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    private void BuildDashboard()
    {
        Controls.Clear();
        _layoutControl = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(_layoutControl);

        var root = _layoutControl.Root;
        root.Padding = new DevExpress.XtraLayout.Utils.Padding(10);

        // Özet Kartları
        var summaryGroup = new LayoutControlGroup { Text = "Aylık Özet", Padding = new DevExpress.XtraLayout.Utils.Padding(5) };
        root.AddItem(summaryGroup);

        AddSummaryCard(summaryGroup, "Gelir", _summary!.TotalIncome, Color.FromArgb(40, 167, 69));
        AddSummaryCard(summaryGroup, "Gider", _summary.TotalExpense, Color.FromArgb(220, 53, 69));
        AddSummaryCard(summaryGroup, "Net", _summary.NetBalance, _summary.NetBalance >= 0 ? Color.FromArgb(40, 167, 69) : Color.FromArgb(220, 53, 69));
        AddSummaryCard(summaryGroup, "Toplam Varlık", _summary.NetWorth, Color.FromArgb(0, 123, 255));

        // Gauge - Bütçe Durumu
        var gaugeGroup = new LayoutControlGroup { Text = "Bütçe Durumu", Padding = new DevExpress.XtraLayout.Utils.Padding(5) };
        root.AddItem(gaugeGroup);

        foreach (var budget in _summary.BudgetStatuses.Take(3))
        {
            var gauge = CreateBudgetGauge(budget);
            var item = new LayoutControlItem { Control = gauge, TextVisible = false };
            gaugeGroup.AddItem(item);
        }

        // Pasta Grafik - Kategori Dağılımı
        var pieChart = CreatePieChart();
        var pieItem = new LayoutControlItem { Control = pieChart, Text = "Kategori Dağılımı" };
        pieItem.SizeConstraintsType = SizeConstraintsType.Custom;
        pieItem.MinSize = new Size(400, 300);
        root.AddItem(pieItem);

        // Çizgi Grafik - Aylık Trend
        var lineChart = CreateLineChart();
        var lineItem = new LayoutControlItem { Control = lineChart, Text = "Aylık Trend" };
        lineItem.SizeConstraintsType = SizeConstraintsType.Custom;
        lineItem.MinSize = new Size(500, 300);
        root.AddItem(lineItem);

        // Hesap Bakiyeleri
        var accountGroup = new LayoutControlGroup { Text = "Hesap Bakiyeleri", Padding = new DevExpress.XtraLayout.Utils.Padding(5) };
        root.AddItem(accountGroup);

        foreach (var account in _summary.AccountBalances)
        {
            var lbl = new LabelControl { Text = $"{account.AccountName}: {account.Balance:N2} {account.CurrencyCode}" };
            lbl.Appearance.Font = new Font("Segoe UI", 11);
            var item = new LayoutControlItem { Control = lbl, TextVisible = false };
            accountGroup.AddItem(item);
        }

        // Tahmin
        var forecastGroup = new LayoutControlGroup { Text = "Ay Sonu Tahmini", Padding = new DevExpress.XtraLayout.Utils.Padding(5) };
        root.AddItem(forecastGroup);

        var lblForecast = new LabelControl
        {
            Text = $"Tahmini Bakiye: {_forecast!.ProjectedEndOfMonthBalance:N2} TRY\n" +
                   $"Günlük Ortalama Harcama: {_forecast.AverageDailySpending:N2} TRY\n" +
                   $"Kalan Gün: {_forecast.DaysRemaining}"
        };
        lblForecast.Appearance.Font = new Font("Segoe UI", 11);
        forecastGroup.AddItem(new LayoutControlItem { Control = lblForecast, TextVisible = false });

        // Yaklaşan İşlemler
        var upcomingGroup = new LayoutControlGroup { Text = "Yaklaşan İşlemler (7 gün)", Padding = new DevExpress.XtraLayout.Utils.Padding(5) };
        root.AddItem(upcomingGroup);

        foreach (var upcoming in _summary.UpcomingTransactions.Take(5))
        {
            var lbl = new LabelControl
            {
                Text = $"📅 {upcoming.DueDate:dd.MM} - {upcoming.Description}: {upcoming.Amount:N2} TRY"
            };
            upcomingGroup.AddItem(new LayoutControlItem { Control = lbl, TextVisible = false });
        }
    }

    private void AddSummaryCard(LayoutControlGroup group, string title, decimal amount, Color color)
    {
        var panel = new PanelControl { Size = new Size(180, 80) };
        panel.Appearance.BackColor = color;
        panel.Appearance.Options.UseBackColor = true;

        var lblTitle = new LabelControl
        {
            Text = title,
            Location = new Point(10, 10),
            Appearance = { ForeColor = Color.White, Font = new Font("Segoe UI", 10) }
        };

        var lblAmount = new LabelControl
        {
            Text = $"{amount:N2} ₺",
            Location = new Point(10, 35),
            Appearance = { ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold) }
        };

        panel.Controls.AddRange(new Control[] { lblTitle, lblAmount });

        var item = new LayoutControlItem { Control = panel, TextVisible = false };
        item.SizeConstraintsType = SizeConstraintsType.Custom;
        item.MinSize = new Size(190, 90);
        item.MaxSize = new Size(200, 100);
        group.AddItem(item);
    }

    private GaugeControl CreateBudgetGauge(BudgetStatusDto budget)
    {
        var gaugeControl = new GaugeControl { Size = new Size(150, 150) };
        var circularGauge = gaugeControl.AddCircularGauge();

        circularGauge.AddScale().Labels.Add(new ArcScaleLabel());
        var scale = circularGauge.Scales[0];
        scale.MinValue = 0;
        scale.MaxValue = 100;
        scale.Value = (float)Math.Min(budget.Percentage, 100);

        scale.AppearanceScale.Brush = new DevExpress.XtraGauges.Core.Drawing.SolidBrushObject(
            budget.Percentage > 90 ? Color.Red : budget.Percentage > 70 ? Color.Orange : Color.Green);

        var label = new LabelComponent { Text = $"{budget.CategoryName}\n{budget.Percentage:F0}%" };
        circularGauge.Labels.Add(label);

        return gaugeControl;
    }

    private ChartControl CreatePieChart()
    {
        var chart = new ChartControl { Size = new Size(400, 300) };
        var series = new Series("Kategoriler", ViewType.Pie);

        foreach (var cat in _summary!.CategorySpendings.Take(8))
        {
            series.Points.Add(new SeriesPoint(cat.CategoryName, (double)cat.Amount));
        }

        ((PieSeriesView)series.View).ExplodeMode = PieExplodeMode.UsePoints;
        chart.Series.Add(series);
        chart.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;

        return chart;
    }

    private ChartControl CreateLineChart()
    {
        var chart = new ChartControl { Size = new Size(500, 300) };

        var seriesIncome = new Series("Gelir", ViewType.Line);
        var seriesExpense = new Series("Gider", ViewType.Line);

        foreach (var trend in _summary!.MonthlyTrends)
        {
            seriesIncome.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Income));
            seriesExpense.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Expense));
        }

        ((LineSeriesView)seriesIncome.View).Color = Color.Green;
        ((LineSeriesView)seriesExpense.View).Color = Color.Red;

        chart.Series.AddRange(new[] { seriesIncome, seriesExpense });
        chart.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;

        return chart;
    }

    public void RefreshData() => _ = LoadDataAsync();
}

