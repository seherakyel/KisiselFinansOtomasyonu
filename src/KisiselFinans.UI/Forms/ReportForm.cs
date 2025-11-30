using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;
using ScottPlot;
using ScottPlot.WinForms;
using SysColor = System.Drawing.Color;
using WinLabel = System.Windows.Forms.Label;

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
        Text = "Raporlar";
        Size = new Size(1100, 750);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = AppTheme.PrimaryDark;

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 70 };
        header.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, header.Width, header.Height);
            AppTheme.DrawGradientBackground(e.Graphics, rect, false);
        };

        var lblTitle = new WinLabel
        {
            Text = "Finansal Raporlar",
            Font = new Font("Segoe UI Light", 24),
            ForeColor = SysColor.White,
            Location = new Point(35, 16),
            AutoSize = true
        };

        var btnClose = new WinLabel
        {
            Text = "X",
            Font = new Font("Segoe UI", 16),
            ForeColor = SysColor.White,
            Size = new Size(50, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnClose.Click += (s, e) => Close();

        header.Resize += (s, e) => btnClose.Location = new Point(header.Width - 60, 10);
        header.Controls.AddRange(new Control[] { lblTitle, btnClose });

        // Loading Label
        var lblLoading = new WinLabel
        {
            Text = "Rapor yukleniyor...",
            Font = new Font("Segoe UI", 14),
            ForeColor = AppTheme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(lblLoading);
        Controls.Add(header);
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
            BeginInvoke(() => MessageBox.Show($"Rapor yuklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    private void BuildReportUI()
    {
        Controls.Clear();

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 70 };
        header.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, header.Width, header.Height);
            AppTheme.DrawGradientBackground(e.Graphics, rect, false);
        };

        var lblTitle = new WinLabel
        {
            Text = "Finansal Raporlar",
            Font = new Font("Segoe UI Light", 24),
            ForeColor = SysColor.White,
            Location = new Point(35, 16),
            AutoSize = true
        };

        var btnClose = new WinLabel
        {
            Text = "X",
            Font = new Font("Segoe UI", 16),
            ForeColor = SysColor.White,
            Size = new Size(50, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnClose.Click += (s, e) => Close();
        header.Resize += (s, e) => btnClose.Location = new Point(header.Width - 60, 10);
        header.Controls.AddRange(new Control[] { lblTitle, btnClose });

        // Tab Control
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11),
            Padding = new Point(20, 10)
        };

        // Style TabControl
        tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabControl.ItemSize = new Size(180, 45);
        tabControl.DrawItem += (s, e) =>
        {
            var tab = tabControl.TabPages[e.Index];
            var isSelected = e.Index == tabControl.SelectedIndex;

            using var bgBrush = new SolidBrush(isSelected ? AppTheme.GradientStart : AppTheme.CardBg);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            using var textBrush = new SolidBrush(isSelected ? SysColor.White : AppTheme.TextSecondary);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tab.Text, e.Font!, textBrush, e.Bounds, sf);
        };

        // Aylik Trend Tab
        var tabMonthly = new TabPage("Aylik Trend") { BackColor = AppTheme.CardBg };
        tabMonthly.Controls.Add(CreateMonthlyChart());
        tabControl.TabPages.Add(tabMonthly);

        // Kategori Dagilimi Tab
        var tabCategory = new TabPage("Kategori Dagilimi") { BackColor = AppTheme.CardBg };
        tabCategory.Controls.Add(CreateCategoryChart());
        tabControl.TabPages.Add(tabCategory);

        // Export Panel
        var exportPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = AppTheme.CardBg,
            Padding = new Padding(30, 15, 30, 15)
        };

        var btnExportCsv = new Button
        {
            Text = "CSV OLARAK KAYDET",
            Location = new Point(30, 15),
            Size = new Size(200, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentGreen,
            ForeColor = AppTheme.PrimaryDark,
            Font = new Font("Segoe UI Semibold", 11),
            Cursor = Cursors.Hand
        };
        btnExportCsv.FlatAppearance.BorderSize = 0;
        btnExportCsv.Click += (s, e) => ExportToCsv();

        var btnPrint = new Button
        {
            Text = "YAZDIR",
            Location = new Point(250, 15),
            Size = new Size(120, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font("Segoe UI Semibold", 11),
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;

        exportPanel.Controls.AddRange(new Control[] { btnExportCsv, btnPrint });

        Controls.Add(tabControl);
        Controls.Add(exportPanel);
        Controls.Add(header);
    }

    private FormsPlot CreateMonthlyChart()
    {
        var plotView = new FormsPlot { Dock = DockStyle.Fill };

        if (_summary?.MonthlyTrends != null && _summary.MonthlyTrends.Any())
        {
            var incomeData = _summary.MonthlyTrends.Select(t => (double)t.Income).ToArray();
            var expenseData = _summary.MonthlyTrends.Select(t => (double)t.Expense).ToArray();
            var months = _summary.MonthlyTrends.Select(t => t.MonthName).ToArray();

            double[] positions = Enumerable.Range(0, incomeData.Length).Select(i => (double)i).ToArray();

            var barIncome = plotView.Plot.Add.Bars(positions.Select(p => p - 0.2).ToArray(), incomeData);
            barIncome.Color = ScottPlot.Color.FromHex("#34D399");
            barIncome.LegendText = "Gelir";

            var barExpense = plotView.Plot.Add.Bars(positions.Select(p => p + 0.2).ToArray(), expenseData);
            barExpense.Color = ScottPlot.Color.FromHex("#FB7185");
            barExpense.LegendText = "Gider";

            plotView.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                positions.Select((p, i) => new ScottPlot.Tick(p, months[i])).ToArray()
            );

            plotView.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#16161F");
            plotView.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1C1C26");
            plotView.Plot.Axes.Color(ScottPlot.Color.FromHex("#A0A0B9"));
            plotView.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#26263A");
            plotView.Plot.ShowLegend(Alignment.UpperRight);
            plotView.Refresh();
        }

        return plotView;
    }

    private FormsPlot CreateCategoryChart()
    {
        var plotView = new FormsPlot { Dock = DockStyle.Fill };

        if (_summary?.CategorySpendings != null && _summary.CategorySpendings.Any())
        {
            var values = _summary.CategorySpendings.Take(8).Select(c => (double)c.Amount).ToArray();
            var labels = _summary.CategorySpendings.Take(8).Select(c => c.CategoryName).ToArray();

            var pie = plotView.Plot.Add.Pie(values);
            pie.ExplodeFraction = 0.03;

            var colors = new[] { "#4F46E5", "#9333EA", "#EC4899", "#38BDF8", "#34D399", "#FB7185", "#FACC15", "#C084FC" };
            for (int i = 0; i < Math.Min(pie.Slices.Count, labels.Length); i++)
            {
                pie.Slices[i].Label = labels[i];
                pie.Slices[i].LabelFontColor = ScottPlot.Color.FromHex("#FFFFFF");
                pie.Slices[i].FillColor = ScottPlot.Color.FromHex(colors[i % colors.Length]);
            }

            plotView.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#16161F");
            plotView.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#16161F");
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
            var lines = new List<string> { "Kategori;Tutar;Yuzde" };
            if (_summary?.CategorySpendings != null)
            {
                lines.AddRange(_summary.CategorySpendings.Select(c =>
                    $"{c.CategoryName};{c.Amount:N2};{c.Percentage:F2}%"));
            }

            lines.Add("");
            lines.Add("Ay;Gelir;Gider;Bakiye");
            if (_summary?.MonthlyTrends != null)
            {
                lines.AddRange(_summary.MonthlyTrends.Select(t =>
                    $"{t.MonthName};{t.Income:N2};{t.Expense:N2};{t.Balance:N2}"));
            }

            File.WriteAllLines(dialog.FileName, lines, System.Text.Encoding.UTF8);
            MessageBox.Show("CSV dosyasi olusturuldu!", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
