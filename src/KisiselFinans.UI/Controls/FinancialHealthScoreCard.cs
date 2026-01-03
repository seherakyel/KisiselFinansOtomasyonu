using System.Drawing.Drawing2D;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Finansal Sağlık Skoru - Daire grafik ile 0-100 arası puan
/// </summary>
public class FinancialHealthScoreCard : UserControl
{
    private readonly int _userId;
    private int _score = 0;
    private int _animatedScore = 0;
    private string _grade = "?";
    private string _recommendation = "";
    private readonly System.Windows.Forms.Timer _animationTimer;
    private Color _scoreColor = AppTheme.AccentBlue;

    public FinancialHealthScoreCard(int userId)
    {
        _userId = userId;
        _animationTimer = new System.Windows.Forms.Timer { Interval = 20 };
        _animationTimer.Tick += AnimationTick;

        InitializeComponent();
        _ = CalculateScoreAsync();
    }

    private void InitializeComponent()
    {
        Size = new Size(320, 200);
        BackColor = AppTheme.PrimaryMedium;
        DoubleBuffered = true;
        Paint += OnPaint;
    }

    private async Task CalculateScoreAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);

            var accounts = await unitOfWork.Accounts.FindAsync(a => a.UserId == _userId);
            var accountIds = accounts.Select(a => a.Id).ToList();

            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var threeMonthsAgo = DateTime.Today.AddMonths(-3);

            var transactions = await unitOfWork.Transactions.FindAsync(t =>
                accountIds.Contains(t.AccountId) &&
                t.TransactionDate >= threeMonthsAgo);

            var txList = transactions.ToList();

            // Son 3 ay gelir/gider
            var totalIncome = txList.Where(t => t.TransactionType == 1).Sum(t => t.Amount);
            var totalExpense = txList.Where(t => t.TransactionType == 2).Sum(t => t.Amount);

            // Net varlık
            var netWorth = accounts.Sum(a => a.CurrentBalance);

            // Skor hesaplama
            int score = 50; // Başlangıç

            // 1. Gelir-Gider dengesi (max 30 puan)
            if (totalIncome > 0)
            {
                var ratio = (totalIncome - totalExpense) / totalIncome;
                score += (int)Math.Min(30, Math.Max(0, ratio * 100));
            }

            // 2. Net varlık pozitif mi? (max 20 puan)
            if (netWorth > 0)
            {
                score += Math.Min(20, (int)(netWorth / 1000)); // Her 1000 TL için 1 puan
            }
            else
            {
                score -= 10;
            }

            // 3. Düzenli tasarruf (Bu ay gelir > gider?) (max 10 puan)
            var thisMonthIncome = txList.Where(t => t.TransactionType == 1 && t.TransactionDate >= startOfMonth).Sum(t => t.Amount);
            var thisMonthExpense = txList.Where(t => t.TransactionType == 2 && t.TransactionDate >= startOfMonth).Sum(t => t.Amount);

            if (thisMonthIncome > thisMonthExpense)
            {
                score += 10;
            }

            // Sınırla
            score = Math.Max(0, Math.Min(100, score));

            // Grade ve öneri belirle
            (_grade, _recommendation, _scoreColor) = score switch
            {
                >= 90 => ("A+", "Mükemmel! Finansal durumunuz çok sağlıklı.", AppTheme.AccentGreen),
                >= 80 => ("A", "Harika! Tasarruflarınıza devam edin.", Color.FromArgb(139, 195, 74)),
                >= 70 => ("B", "İyi gidiyorsunuz. Biraz daha tasarruf yapabilirsiniz.", AppTheme.AccentCyan),
                >= 60 => ("C", "Ortalama. Harcamalarınızı gözden geçirin.", AppTheme.AccentYellow),
                >= 50 => ("D", "Dikkat! Gelir-gider dengenizi iyileştirin.", AppTheme.AccentOrange),
                _ => ("F", "Uyarı! Acil olarak bütçe planı yapın.", AppTheme.AccentRed)
            };

            _score = score;
            _animatedScore = 0;
            _animationTimer.Start();

            BeginInvoke(() => Invalidate());
        }
        catch (Exception ex)
        {
            _grade = "?";
            _recommendation = $"Hesaplanamadı: {ex.Message}";
            BeginInvoke(() => Invalidate());
        }
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        if (_animatedScore < _score)
        {
            _animatedScore += 2;
            if (_animatedScore > _score) _animatedScore = _score;
            Invalidate();
        }
        else
        {
            _animationTimer.Stop();
        }
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Başlık
        using var titleFont = new Font("Segoe UI Semibold", 12);
        g.DrawString("💪 Finansal Sağlık Skoru", titleFont, new SolidBrush(AppTheme.TextPrimary), 15, 12);

        // Daire grafik
        int circleSize = 100;
        int circleX = 30;
        int circleY = 50;

        // Arka plan daire
        using var bgPen = new Pen(AppTheme.Surface, 10);
        g.DrawArc(bgPen, circleX, circleY, circleSize, circleSize, 0, 360);

        // Progress daire
        using var progressPen = new Pen(_scoreColor, 10);
        progressPen.StartCap = LineCap.Round;
        progressPen.EndCap = LineCap.Round;
        float sweepAngle = (float)(_animatedScore / 100.0 * 360);
        g.DrawArc(progressPen, circleX, circleY, circleSize, circleSize, -90, sweepAngle);

        // Skor metni (ortada)
        using var scoreFont = new Font("Segoe UI Bold", 24);
        var scoreText = _animatedScore.ToString();
        var scoreSize = g.MeasureString(scoreText, scoreFont);
        g.DrawString(scoreText, scoreFont, new SolidBrush(_scoreColor),
            circleX + circleSize / 2 - scoreSize.Width / 2,
            circleY + circleSize / 2 - scoreSize.Height / 2);

        // Grade (sağ taraf)
        using var gradeFont = new Font("Segoe UI Bold", 36);
        g.DrawString(_grade, gradeFont, new SolidBrush(_scoreColor), 160, 55);

        // Grade label
        using var labelFont = new Font("Segoe UI", 10);
        g.DrawString("DERECE", labelFont, new SolidBrush(AppTheme.TextMuted), 163, 105);

        // Öneri (alt kısım)
        using var recFont = new Font("Segoe UI", 9);
        var recBrush = new SolidBrush(AppTheme.TextSecondary);

        // Text wrap için
        var recRect = new RectangleF(15, 160, Width - 30, 35);
        g.DrawString(_recommendation, recFont, recBrush, recRect);

        // Border
        using var borderPen = new Pen(Color.FromArgb(50, _scoreColor), 1);
        using var borderPath = CreateRoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10);
        g.DrawPath(borderPen, borderPath);
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

    public void RefreshData() => _ = CalculateScoreAsync();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}

