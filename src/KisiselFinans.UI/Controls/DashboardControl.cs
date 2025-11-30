using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;
using ScottPlot;
using ScottPlot.WinForms;
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

        // Özet Kartları
        AddSummaryCard("💵 Aylık Gelir", _summary!.TotalIncome, AppTheme.AccentGreen);
        AddSummaryCard("💸 Aylık Gider", _summary.TotalExpense, AppTheme.AccentRed);
        AddSummaryCard("📊 Net Bakiye", _summary.NetBalance, _summary.NetBalance >= 0 ? AppTheme.AccentGreen : AppTheme.AccentRed);
        AddSummaryCard("🏦 Toplam Varlık", _summary.NetWorth, AppTheme.AccentBlue);

        // Tahmin Kartı
        AddForecastCard();

        // Pasta Grafik
        AddPieChart();

        // Çizgi Grafik
        AddLineChart();

        // Hesap Bakiyeleri
        AddAccountsList();

        // Bütçe Durumu
        AddBudgetsList();

        // Yaklaşan İşlemler
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

        var plotView = new FormsPlot
        {
            Dock = DockStyle.Fill
        };

        var values = _summary!.CategorySpendings.Take(6).Select(c => (double)c.Amount).ToArray();
        var labels = _summary.CategorySpendings.Take(6).Select(c => c.CategoryName).ToArray();

        if (values.Length > 0)
        {
            var pie = plotView.Plot.Add.Pie(values);
            pie.ExplodeFraction = 0.05;
            pie.SliceLabelDistance = 1.2;
            
            for (int i = 0; i < Math.Min(pie.Slices.Count, labels.Length); i++)
            {
                pie.Slices[i].Label = labels[i];
                pie.Slices[i].LabelFontColor = ScottPlot.Color.FromHex("#FFFFFF");
            }

            plotView.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotView.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotView.Plot.HideAxesAndGrid();
            plotView.Refresh();
        }

        card.Controls.Add(plotView);
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

        var plotView = new FormsPlot
        {
            Dock = DockStyle.Fill
        };

        var incomeData = _summary!.MonthlyTrends.Select(t => (double)t.Income).ToArray();
        var expenseData = _summary.MonthlyTrends.Select(t => (double)t.Expense).ToArray();
        var months = _summary.MonthlyTrends.Select(t => t.MonthName).ToArray();

        if (incomeData.Length > 0)
        {
            double[] xs = Enumerable.Range(0, incomeData.Length).Select(i => (double)i).ToArray();

            var incomeLine = plotView.Plot.Add.Scatter(xs, incomeData);
            incomeLine.Color = ScottPlot.Color.FromHex("#4CAF50");
            incomeLine.LineWidth = 3;
            incomeLine.LegendText = "Gelir";

            var expenseLine = plotView.Plot.Add.Scatter(xs, expenseData);
            expenseLine.Color = ScottPlot.Color.FromHex("#F44336");
            expenseLine.LineWidth = 3;
            expenseLine.LegendText = "Gider";

            plotView.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                xs.Select((x, i) => new ScottPlot.Tick(x, months[i])).ToArray()
            );

            plotView.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotView.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#2D2D2D");
            plotView.Plot.Axes.Color(ScottPlot.Color.FromHex("#B4B4B4"));
            plotView.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#3D3D3D");
            plotView.Plot.ShowLegend(Alignment.UpperRight);
            plotView.Refresh();
        }

        card.Controls.Add(plotView);
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
