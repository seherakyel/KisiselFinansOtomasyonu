using System.IO;
using DevExpress.XtraCharts;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;
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

        var lblLoading = new WinLabel
        {
            Text = "Rapor yükleniyor...",
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
            BeginInvoke(() => MessageBox.Show($"Rapor yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    private void BuildReportUI()
    {
        Controls.Clear();

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

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11),
            Padding = new Point(20, 10)
        };

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

        var tabMonthly = new TabPage("Aylık Trend") { BackColor = AppTheme.CardBg };
        tabMonthly.Controls.Add(CreateMonthlyChart());
        tabControl.TabPages.Add(tabMonthly);

        var tabCategory = new TabPage("Kategori Dağılımı") { BackColor = AppTheme.CardBg };
        tabCategory.Controls.Add(CreateCategoryChart());
        tabControl.TabPages.Add(tabCategory);

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

    private ChartControl CreateMonthlyChart()
    {
        var chartControl = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardBg
        };

        if (_summary?.MonthlyTrends != null && _summary.MonthlyTrends.Any())
        {
            var seriesIncome = new Series("Gelir", ViewType.Bar);
            var seriesExpense = new Series("Gider", ViewType.Bar);

            foreach (var trend in _summary.MonthlyTrends)
            {
                seriesIncome.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Income));
                seriesExpense.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Expense));
            }

            chartControl.Series.AddRange(new[] { seriesIncome, seriesExpense });

            var incomeView = (BarSeriesView)seriesIncome.View;
            incomeView.Color = SysColor.FromArgb(52, 211, 153);

            var expenseView = (BarSeriesView)seriesExpense.View;
            expenseView.Color = SysColor.FromArgb(251, 113, 133);

            var diagram = (XYDiagram)chartControl.Diagram;
            diagram.DefaultPane.BackColor = AppTheme.CardBg;
            diagram.AxisX.Label.TextColor = AppTheme.TextSecondary;
            diagram.AxisY.Label.TextColor = AppTheme.TextSecondary;
            diagram.AxisX.GridLines.Visible = false;

            chartControl.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;
            chartControl.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Right;
            chartControl.Legend.AlignmentVertical = LegendAlignmentVertical.Top;
            chartControl.Legend.BackColor = SysColor.Transparent;
            chartControl.Legend.TextColor = AppTheme.TextPrimary;
        }

        return chartControl;
    }

    private ChartControl CreateCategoryChart()
    {
        var chartControl = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.CardBg
        };

        if (_summary?.CategorySpendings != null && _summary.CategorySpendings.Any())
        {
            var series = new Series("Kategoriler", ViewType.Pie);

            var colors = new[] {
                SysColor.FromArgb(79, 70, 229),
                SysColor.FromArgb(147, 51, 234),
                SysColor.FromArgb(236, 72, 153),
                SysColor.FromArgb(56, 189, 248),
                SysColor.FromArgb(52, 211, 153),
                SysColor.FromArgb(251, 113, 133),
                SysColor.FromArgb(250, 204, 21),
                SysColor.FromArgb(192, 132, 252)
            };

            int colorIndex = 0;
            foreach (var category in _summary.CategorySpendings.Take(8))
            {
                var point = new SeriesPoint(category.CategoryName, (double)category.Amount);
                point.Color = colors[colorIndex % colors.Length];
                series.Points.Add(point);
                colorIndex++;
            }

            chartControl.Series.Add(series);

            var pieView = (PieSeriesView)series.View;
            pieView.ExplodedDistancePercentage = 3;

            series.LabelsVisibility = DevExpress.Utils.DefaultBoolean.True;
            series.Label.TextPattern = "{A}: {VP:P0}";

            chartControl.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;
            chartControl.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Right;
            chartControl.Legend.AlignmentVertical = LegendAlignmentVertical.Center;
            chartControl.Legend.BackColor = SysColor.Transparent;
            chartControl.Legend.TextColor = AppTheme.TextPrimary;
        }

        return chartControl;
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
            MessageBox.Show("CSV dosyası oluşturuldu!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
