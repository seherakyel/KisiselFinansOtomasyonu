using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraLayout;
using DevExpress.XtraPrinting;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class ReportViewerForm : XtraForm
{
    private readonly int _userId;
    private readonly string _reportType;

    public ReportViewerForm(int userId, string reportType)
    {
        _userId = userId;
        _reportType = reportType;
        InitializeComponent();
        _ = LoadReportAsync();
    }

    private void InitializeComponent()
    {
        Text = _reportType == "Monthly" ? "Aylık Rapor" : "Kategori Raporu";
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterParent;
    }

    private async Task LoadReportAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);
        var accountService = new AccountService(unitOfWork);
        var budgetService = new BudgetService(unitOfWork);
        var scheduledService = new ScheduledTransactionService(unitOfWork);
        var dashboardService = new DashboardService(unitOfWork, accountService, budgetService, scheduledService);

        var summary = await dashboardService.GetDashboardSummaryAsync(_userId);

        var layout = new LayoutControl { Dock = DockStyle.Fill };
        Controls.Add(layout);

        if (_reportType == "Monthly")
        {
            var lineChart = CreateMonthlyChart(summary.MonthlyTrends);
            var chartItem = new LayoutControlItem { Control = lineChart, Text = "Aylık Gelir/Gider Trendi" };
            chartItem.SizeConstraintsType = SizeConstraintsType.Custom;
            chartItem.MinSize = new Size(800, 400);
            layout.Root.AddItem(chartItem);
        }
        else
        {
            var pieChart = CreateCategoryChart(summary.CategorySpendings);
            var chartItem = new LayoutControlItem { Control = pieChart, Text = "Kategori Dağılımı" };
            chartItem.SizeConstraintsType = SizeConstraintsType.Custom;
            chartItem.MinSize = new Size(600, 400);
            layout.Root.AddItem(chartItem);
        }

        var btnExportPdf = new SimpleButton { Text = "PDF Olarak Kaydet", Size = new Size(150, 30) };
        btnExportPdf.Click += (s, e) => ExportToPdf();
        layout.Root.AddItem(new LayoutControlItem { Control = btnExportPdf, TextVisible = false });

        var btnExportExcel = new SimpleButton { Text = "Excel Olarak Kaydet", Size = new Size(150, 30) };
        btnExportExcel.Click += (s, e) => ExportToExcel();
        layout.Root.AddItem(new LayoutControlItem { Control = btnExportExcel, TextVisible = false });
    }

    private ChartControl CreateMonthlyChart(List<MonthlyTrendDto> trends)
    {
        var chart = new ChartControl { Size = new Size(800, 400) };

        var seriesIncome = new Series("Gelir", ViewType.Bar);
        var seriesExpense = new Series("Gider", ViewType.Bar);

        foreach (var trend in trends)
        {
            seriesIncome.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Income));
            seriesExpense.Points.Add(new SeriesPoint(trend.MonthName, (double)trend.Expense));
        }

        ((BarSeriesView)seriesIncome.View).Color = Color.Green;
        ((BarSeriesView)seriesExpense.View).Color = Color.Red;

        chart.Series.AddRange(new[] { seriesIncome, seriesExpense });
        chart.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;

        return chart;
    }

    private ChartControl CreateCategoryChart(List<CategorySpendingDto> categories)
    {
        var chart = new ChartControl { Size = new Size(600, 400) };
        var series = new Series("Kategoriler", ViewType.Pie);

        foreach (var cat in categories)
        {
            series.Points.Add(new SeriesPoint(cat.CategoryName, (double)cat.Amount));
        }

        ((PieSeriesView)series.View).ExplodeMode = PieExplodeMode.UseFilters;
        chart.Series.Add(series);
        chart.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;

        return chart;
    }

    private void ExportToPdf()
    {
        using var dialog = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"Rapor_{DateTime.Now:yyyyMMdd}.pdf" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var ps = new PrintingSystem();
            var link = new PrintableComponentLink(ps) { Component = Controls.OfType<LayoutControl>().First() };
            link.CreateDocument();
            ps.ExportToPdf(dialog.FileName);
            XtraMessageBox.Show("PDF oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ExportToExcel()
    {
        using var dialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"Rapor_{DateTime.Now:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var ps = new PrintingSystem();
            var link = new PrintableComponentLink(ps) { Component = Controls.OfType<LayoutControl>().First() };
            link.CreateDocument();
            ps.ExportToXlsx(dialog.FileName);
            XtraMessageBox.Show("Excel oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

