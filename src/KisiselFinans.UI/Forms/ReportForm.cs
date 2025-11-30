using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;
using ScottPlot;
using ScottPlot.WinForms;

namespace KisiselFinans.UI.Forms;

public class ReportForm : Form
{
    private readonly int _userId;
    private DashboardSummaryDto? _summary;

    public ReportForm(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadReportAsync();
    }

    private void InitializeComponent()
    {
        Text = "📊 Raporlar";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.PrimaryDark;
    }

    private async Task LoadReportAsync()
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

            BeginInvoke(BuildReportUI);
        }
        catch (Exception ex)
        {
            BeginInvoke(() => MessageBox.Show($"Rapor yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    private void BuildReportUI()
    {
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontBody
        };

        // Aylık Trend Tab
        var tabMonthly = new TabPage("📈 Aylık Trend") { BackColor = AppTheme.PrimaryDark };
        tabMonthly.Controls.Add(CreateMonthlyChart());
        tabControl.TabPages.Add(tabMonthly);

        // Kategori Dağılımı Tab
        var tabCategory = new TabPage("🥧 Kategori Dağılımı") { BackColor = AppTheme.PrimaryDark };
        tabCategory.Controls.Add(CreateCategoryChart());
        tabControl.TabPages.Add(tabCategory);

        // Export Button Panel
        var btnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = AppTheme.PrimaryMedium,
            Padding = new Padding(20, 10, 20, 10)
        };

        var btnExportCsv = new Button
        {
            Text = "📤 CSV Olarak Kaydet",
            Location = new Point(20, 8),
            Size = new Size(160, 32)
        };
        AppTheme.StyleButton(btnExportCsv, true);
        btnExportCsv.Click += (s, e) => ExportToCsv();

        btnPanel.Controls.Add(btnExportCsv);

        Controls.Add(tabControl);
        Controls.Add(btnPanel);
    }

    private FormsPlot CreateMonthlyChart()
    {
        var plotView = new FormsPlot { Dock = DockStyle.Fill };

        var incomeData = _summary!.MonthlyTrends.Select(t => (double)t.Income).ToArray();
        var expenseData = _summary.MonthlyTrends.Select(t => (double)t.Expense).ToArray();
        var months = _summary.MonthlyTrends.Select(t => t.MonthName).ToArray();

        if (incomeData.Length > 0)
        {
            double[] positions = Enumerable.Range(0, incomeData.Length).Select(i => (double)i).ToArray();

            var barIncome = plotView.Plot.Add.Bars(positions.Select(p => p - 0.2).ToArray(), incomeData);
            barIncome.Color = ScottPlot.Color.FromHex("#4CAF50");
            barIncome.LegendText = "Gelir";

            var barExpense = plotView.Plot.Add.Bars(positions.Select(p => p + 0.2).ToArray(), expenseData);
            barExpense.Color = ScottPlot.Color.FromHex("#F44336");
            barExpense.LegendText = "Gider";

            plotView.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                positions.Select((p, i) => new ScottPlot.Tick(p, months[i])).ToArray()
            );

            plotView.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#121212");
            plotView.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plotView.Plot.Axes.Color(ScottPlot.Color.FromHex("#B4B4B4"));
            plotView.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#3D3D3D");
            plotView.Plot.ShowLegend(Alignment.UpperRight);
            plotView.Refresh();
        }

        return plotView;
    }

    private FormsPlot CreateCategoryChart()
    {
        var plotView = new FormsPlot { Dock = DockStyle.Fill };

        var values = _summary!.CategorySpendings.Take(8).Select(c => (double)c.Amount).ToArray();
        var labels = _summary.CategorySpendings.Take(8).Select(c => c.CategoryName).ToArray();

        if (values.Length > 0)
        {
            var pie = plotView.Plot.Add.Pie(values);
            pie.ExplodeFraction = 0.05;
            
            for (int i = 0; i < Math.Min(pie.Slices.Count, labels.Length); i++)
            {
                pie.Slices[i].Label = labels[i];
                pie.Slices[i].LabelFontColor = ScottPlot.Color.FromHex("#FFFFFF");
            }

            plotView.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#121212");
            plotView.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#121212");
            plotView.Plot.HideAxesAndGrid();
            plotView.Refresh();
        }

        return plotView;
    }

    private void ExportToCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV Files|*.csv",
            FileName = $"Rapor_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var lines = new List<string> { "Kategori;Tutar;Yüzde" };
            lines.AddRange(_summary!.CategorySpendings.Select(c =>
                $"{c.CategoryName};{c.Amount:N2};{c.Percentage:F2}%"));

            lines.Add("");
            lines.Add("Ay;Gelir;Gider;Bakiye");
            lines.AddRange(_summary.MonthlyTrends.Select(t =>
                $"{t.MonthName};{t.Income:N2};{t.Expense:N2};{t.Balance:N2}"));

            File.WriteAllLines(dialog.FileName, lines, System.Text.Encoding.UTF8);
            MessageBox.Show("CSV dosyası oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

