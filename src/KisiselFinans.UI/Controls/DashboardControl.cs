using DevExpress.XtraCharts;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;
using SysColor = System.Drawing.Color;
using WinLabel = System.Windows.Forms.Label;

namespace KisiselFinans.UI.Controls;

public class DashboardControl : UserControl
{
    private readonly int _userId;
    private DashboardSummaryDto? _summary;
    private ForecastDto? _forecast;
    private FlowLayoutPanel _mainLayout = null!;

    public DashboardControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.PrimaryDark;
        AutoScroll = true;

        _mainLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10)
        };

        var lblLoading = new WinLabel
        {
            Text = "⏳ Veriler yükleniyor...",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _mainLayout.Controls.Add(lblLoading);

        Controls.Add(_mainLayout);
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

            BeginInvoke(BuildDashboard);
        }
        catch (Exception ex)
        {
            BeginInvoke(() => MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    private void BuildDashboard()
    {
        _mainLayout.Controls.Clear();

        AddSummaryCard("💵 Aylık Gelir", _summary!.TotalIncome, AppTheme.AccentGreen);
        AddSummaryCard("💸 Aylık Gider", _summary.TotalExpense, AppTheme.AccentRed);
        AddSummaryCard("📊 Net Bakiye", _summary.NetBalance, _summary.NetBalance >= 0 ? AppTheme.AccentGreen : AppTheme.AccentRed);
        AddSummaryCard("🏦 Toplam Varlık", _summary.NetWorth, AppTheme.AccentBlue);

        AddForecastCard();
        AddPieChart();
        AddLineChart();
        AddAccountsList();
        AddBudgetsList();
        AddUpcomingList();
    }

    private void AddSummaryCard(string title, decimal amount, SysColor accentColor)
    {
        var card = new Panel
        {
            Size = new Size(260, 100),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var stripe = new Panel
        {
            Size = new Size(5, 100),
            BackColor = accentColor,
            Dock = DockStyle.Left
        };

        var lblTitle = new WinLabel
        {
            Text = title,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(20, 15),
            AutoSize = true
        };

        var lblAmount = new WinLabel
        {
            Text = $"₺ {amount:N2}",
            Font = new Font("Segoe UI Semibold", 22),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 45),
            AutoSize = true
        };

        card.Controls.AddRange(new Control[] { stripe, lblTitle, lblAmount });
        _mainLayout.Controls.Add(card);
    }

    private void AddForecastCard()
    {
        var card = new Panel
        {
            Size = new Size(540, 100),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var stripe = new Panel
        {
            Size = new Size(5, 100),
            BackColor = AppTheme.AccentPurple,
            Dock = DockStyle.Left
        };

        var lblTitle = new WinLabel
        {
            Text = "🔮 Ay Sonu Tahmini",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(20, 10),
            AutoSize = true
        };

        var lblForecast = new WinLabel
        {
            Text = $"Tahmini Bakiye: ₺ {_forecast!.ProjectedEndOfMonthBalance:N2}   |   " +
                   $"Günlük Ort. Harcama: ₺ {_forecast.AverageDailySpending:N2}   |   " +
                   $"Kalan Gün: {_forecast.DaysRemaining}",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 40),
            AutoSize = true
        };

        card.Controls.AddRange(new Control[] { stripe, lblTitle, lblForecast });
        _mainLayout.Controls.Add(card);
    }

    private void AddPieChart()
    {
        var card = new Panel
        {
            Size = new Size(400, 320),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var lblTitle = new WinLabel
        {
            Text = "🥧 Kategori Dağılımı",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        var chartControl = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.PrimaryMedium
        };

        var series = new Series("Kategoriler", ViewType.Pie);
        
        foreach (var category in _summary!.CategorySpendings.Take(6))
        {
            series.Points.Add(new SeriesPoint(category.CategoryName, (double)category.Amount));
        }

        chartControl.Series.Add(series);

        var pieView = (PieSeriesView)series.View;
        pieView.ExplodedDistancePercentage = 5;

        chartControl.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;
        chartControl.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Right;
        chartControl.Legend.AlignmentVertical = LegendAlignmentVertical.Center;
        chartControl.Legend.BackColor = SysColor.Transparent;
        chartControl.Legend.TextColor = AppTheme.TextPrimary;

        series.LabelsVisibility = DevExpress.Utils.DefaultBoolean.True;
        series.Label.TextPattern = "{A}: {VP:P0}";

        card.Controls.Add(chartControl);
        card.Controls.Add(lblTitle);
        _mainLayout.Controls.Add(card);
    }

    private void AddLineChart()
    {
        var card = new Panel
        {
            Size = new Size(500, 320),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var lblTitle = new WinLabel
        {
            Text = "📈 Aylık Trend",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        var chartControl = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.PrimaryMedium
        };

        var seriesIncome = new Series("Gelir", ViewType.Line);
        var seriesExpense = new Series("Gider", ViewType.Line);

        foreach (var trend in _summary!.MonthlyTrends)
        {
            seriesIncome.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Income));
            seriesExpense.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Expense));
        }

        chartControl.Series.AddRange(new[] { seriesIncome, seriesExpense });

        var incomeView = (LineSeriesView)seriesIncome.View;
        incomeView.Color = SysColor.FromArgb(76, 175, 80);
        incomeView.LineStyle.Thickness = 3;
        incomeView.MarkerVisibility = DevExpress.Utils.DefaultBoolean.True;

        var expenseView = (LineSeriesView)seriesExpense.View;
        expenseView.Color = SysColor.FromArgb(244, 67, 54);
        expenseView.LineStyle.Thickness = 3;
        expenseView.MarkerVisibility = DevExpress.Utils.DefaultBoolean.True;

        var diagram = (XYDiagram)chartControl.Diagram;
        diagram.DefaultPane.BackColor = AppTheme.PrimaryMedium;
        diagram.AxisX.Label.TextColor = AppTheme.TextSecondary;
        diagram.AxisY.Label.TextColor = AppTheme.TextSecondary;

        chartControl.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;
        chartControl.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Right;
        chartControl.Legend.AlignmentVertical = LegendAlignmentVertical.Top;
        chartControl.Legend.BackColor = SysColor.Transparent;
        chartControl.Legend.TextColor = AppTheme.TextPrimary;

        card.Controls.Add(chartControl);
        card.Controls.Add(lblTitle);
        _mainLayout.Controls.Add(card);
    }

    private void AddAccountsList()
    {
        var card = new Panel
        {
            Size = new Size(350, 200),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var lblTitle = new WinLabel
        {
            Text = "🏦 Hesap Bakiyeleri",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        var listPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true
        };

        foreach (var account in _summary!.AccountBalances.Take(5))
        {
            var row = new WinLabel
            {
                Text = $"{account.AccountName}: ₺ {account.Balance:N2}",
                Font = AppTheme.FontBody,
                ForeColor = account.Balance >= 0 ? AppTheme.TextPrimary : AppTheme.AccentRed,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };
            listPanel.Controls.Add(row);
        }

        card.Controls.Add(listPanel);
        card.Controls.Add(lblTitle);
        _mainLayout.Controls.Add(card);
    }

    private void AddBudgetsList()
    {
        var card = new Panel
        {
            Size = new Size(350, 200),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var lblTitle = new WinLabel
        {
            Text = "🎯 Bütçe Durumu",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        var listPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true
        };

        foreach (var budget in _summary!.BudgetStatuses.Take(4))
        {
            var pnl = new Panel { Size = new Size(300, 35), Margin = new Padding(0, 3, 0, 3) };

            var lbl = new WinLabel
            {
                Text = $"{budget.CategoryName}: {budget.Percentage:F0}%",
                Font = AppTheme.FontSmall,
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };

            var progress = new ProgressBar
            {
                Location = new Point(0, 18),
                Size = new Size(300, 12),
                Value = Math.Min((int)budget.Percentage, 100),
                Style = ProgressBarStyle.Continuous
            };

            pnl.Controls.AddRange(new Control[] { lbl, progress });
            listPanel.Controls.Add(pnl);
        }

        card.Controls.Add(listPanel);
        card.Controls.Add(lblTitle);
        _mainLayout.Controls.Add(card);
    }

    private void AddUpcomingList()
    {
        var card = new Panel
        {
            Size = new Size(350, 200),
            BackColor = AppTheme.PrimaryMedium,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var lblTitle = new WinLabel
        {
            Text = "📅 Yaklaşan İşlemler",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        var listPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true
        };

        foreach (var upcoming in _summary!.UpcomingTransactions.Take(5))
        {
            var row = new WinLabel
            {
                Text = $"📌 {upcoming.DueDate:dd.MM} - {upcoming.Description}: ₺ {upcoming.Amount:N2}",
                Font = AppTheme.FontSmall,
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            listPanel.Controls.Add(row);
        }

        card.Controls.Add(listPanel);
        card.Controls.Add(lblTitle);
        _mainLayout.Controls.Add(card);
    }

    public void RefreshData() => _ = LoadDataAsync();
}
