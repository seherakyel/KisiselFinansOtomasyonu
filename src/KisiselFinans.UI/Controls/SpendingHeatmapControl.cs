using System.Drawing.Drawing2D;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// GitHub tarzı harcama ısı haritası - Son 365 günlük harcama yoğunluğu
/// </summary>
public class SpendingHeatmapControl : UserControl
{
    private readonly int _userId;
    private Dictionary<DateTime, decimal> _dailySpending = new();
    private decimal _maxSpending = 0;
    private ToolTip _tooltip = new();
    private const int CellSize = 14;
    private const int CellMargin = 3;
    private const int WeeksToShow = 53;

    public SpendingHeatmapControl(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Size = new Size(920, 200);
        BackColor = AppTheme.PrimaryMedium;
        DoubleBuffered = true;
        Padding = new Padding(15);

        _tooltip.SetToolTip(this, "");
        _tooltip.BackColor = Color.FromArgb(40, 44, 60);
        _tooltip.ForeColor = Color.White;

        MouseMove += OnMouseMove;
        Paint += OnPaint;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);

            var startDate = DateTime.Today.AddDays(-365);
            var accounts = await unitOfWork.Accounts.FindAsync(a => a.UserId == _userId);
            var accountIds = accounts.Select(a => a.Id).ToList();

            var transactions = await unitOfWork.Transactions.FindAsync(t =>
                accountIds.Contains(t.AccountId) &&
                t.TransactionType == 2 && // Sadece giderler
                t.TransactionDate >= startDate);

            _dailySpending = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            _maxSpending = _dailySpending.Any() ? _dailySpending.Values.Max() : 1;

            BeginInvoke(() => Invalidate());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Heatmap veri yükleme hatası: {ex.Message}");
        }
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Başlık
        using var titleFont = new Font("Segoe UI Semibold", 14);
        g.DrawString("📊 Harcama Isı Haritası", titleFont, new SolidBrush(AppTheme.TextPrimary), 15, 10);

        using var subtitleFont = new Font("Segoe UI", 9);
        g.DrawString("Son 1 yıllık günlük harcama yoğunluğu", subtitleFont, new SolidBrush(AppTheme.TextMuted), 17, 35);

        // Ay isimleri
        DrawMonthLabels(g);

        // Gün isimleri
        DrawDayLabels(g);

        // Hücreleri çiz
        DrawHeatmapCells(g);

        // Legend
        DrawLegend(g);
    }

    private void DrawMonthLabels(Graphics g)
    {
        using var font = new Font("Segoe UI", 8);
        var brush = new SolidBrush(AppTheme.TextMuted);

        string[] months = { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };

        var startDate = DateTime.Today.AddDays(-(WeeksToShow * 7));
        int lastMonth = -1;

        for (int week = 0; week < WeeksToShow; week++)
        {
            var date = startDate.AddDays(week * 7);
            if (date.Month != lastMonth)
            {
                int x = 55 + week * (CellSize + CellMargin);
                g.DrawString(months[date.Month - 1], font, brush, x, 55);
                lastMonth = date.Month;
            }
        }
    }

    private void DrawDayLabels(Graphics g)
    {
        using var font = new Font("Segoe UI", 8);
        var brush = new SolidBrush(AppTheme.TextMuted);

        string[] days = { "Pzt", "", "Çar", "", "Cum", "", "Paz" };
        int y = 75;

        for (int i = 0; i < 7; i++)
        {
            if (!string.IsNullOrEmpty(days[i]))
            {
                g.DrawString(days[i], font, brush, 15, y + i * (CellSize + CellMargin));
            }
        }
    }

    private void DrawHeatmapCells(Graphics g)
    {
        var startDate = DateTime.Today.AddDays(-(WeeksToShow * 7));
        // Haftanın başına (Pazartesi) hizala
        startDate = startDate.AddDays(-(int)startDate.DayOfWeek + 1);
        if (startDate.DayOfWeek == DayOfWeek.Sunday) startDate = startDate.AddDays(-6);

        for (int week = 0; week < WeeksToShow; week++)
        {
            for (int day = 0; day < 7; day++)
            {
                var date = startDate.AddDays(week * 7 + day);
                if (date > DateTime.Today) continue;

                int x = 55 + week * (CellSize + CellMargin);
                int y = 75 + day * (CellSize + CellMargin);

                var spending = _dailySpending.GetValueOrDefault(date, 0);
                var color = GetHeatColor(spending);

                using var brush = new SolidBrush(color);
                using var path = CreateRoundedRect(new Rectangle(x, y, CellSize, CellSize), 3);
                g.FillPath(brush, path);
            }
        }
    }

    private Color GetHeatColor(decimal spending)
    {
        if (spending == 0)
            return AppTheme.Surface; // Boş gün - tema rengi

        var intensity = Math.Min(1.0, (double)(spending / (_maxSpending > 0 ? _maxSpending : 1)));

        // Yeşil tonları (GitHub tarzı) - az harcama iyi, çok harcama kötü
        // Ama finans için tersini yapalım: az=yeşil, çok=kırmızı
        if (intensity < 0.25)
            return Color.FromArgb(46, 160, 67); // Açık yeşil - az harcama
        else if (intensity < 0.5)
            return Color.FromArgb(139, 195, 74); // Yeşil-sarı
        else if (intensity < 0.75)
            return Color.FromArgb(255, 193, 7); // Sarı-turuncu
        else
            return Color.FromArgb(239, 83, 80); // Kırmızı - çok harcama
    }

    private GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void DrawLegend(Graphics g)
    {
        using var font = new Font("Segoe UI", 8);
        var brush = new SolidBrush(AppTheme.TextMuted);

        int x = 700;
        int y = 170;

        g.DrawString("Az", font, brush, x, y);

        Color[] colors = {
            AppTheme.Surface,
            Color.FromArgb(46, 160, 67),
            Color.FromArgb(139, 195, 74),
            Color.FromArgb(255, 193, 7),
            Color.FromArgb(239, 83, 80)
        };

        for (int i = 0; i < colors.Length; i++)
        {
            using var colorBrush = new SolidBrush(colors[i]);
            g.FillRectangle(colorBrush, x + 25 + i * 18, y, 14, 14);
        }

        g.DrawString("Çok", font, brush, x + 25 + colors.Length * 18 + 5, y);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var startDate = DateTime.Today.AddDays(-(WeeksToShow * 7));
        startDate = startDate.AddDays(-(int)startDate.DayOfWeek + 1);
        if (startDate.DayOfWeek == DayOfWeek.Sunday) startDate = startDate.AddDays(-6);

        int week = (e.X - 55) / (CellSize + CellMargin);
        int day = (e.Y - 75) / (CellSize + CellMargin);

        if (week >= 0 && week < WeeksToShow && day >= 0 && day < 7)
        {
            var date = startDate.AddDays(week * 7 + day);
            if (date <= DateTime.Today)
            {
                var spending = _dailySpending.GetValueOrDefault(date, 0);
                _tooltip.SetToolTip(this, $"{date:dd MMMM yyyy}\nHarcama: ₺{spending:N2}");
                return;
            }
        }
        _tooltip.SetToolTip(this, "");
    }

    public void RefreshData() => _ = LoadDataAsync();
}

