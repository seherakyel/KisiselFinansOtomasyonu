using System.Drawing.Drawing2D;
using KisiselFinans.Core.DTOs;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Finansal Sağlık Skoru Kartı ⭐
/// </summary>
public class FinancialHealthCard : UserControl
{
    private FinancialHealthDto? _healthData;
    private int _animatedScore = 0;
    private System.Windows.Forms.Timer _animTimer = null!;

    public FinancialHealthCard()
    {
        Size = new Size(320, 220);
        BackColor = Color.Transparent;
        DoubleBuffered = true;

        _animTimer = new System.Windows.Forms.Timer { Interval = 20 };
        _animTimer.Tick += (s, e) =>
        {
            if (_healthData != null && _animatedScore < _healthData.Score)
            {
                _animatedScore = Math.Min(_animatedScore + 2, _healthData.Score);
                Invalidate();
            }
            else
            {
                _animTimer.Stop();
            }
        };
    }

    public void SetHealth(FinancialHealthDto health)
    {
        _healthData = health;
        _animatedScore = 0;
        _animTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Kart arka planı
        using var bgBrush = new SolidBrush(AppTheme.CardBackground);
        using var path = CreateRoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        g.FillPath(bgBrush, path);

        // Başlık
        using var titleFont = new Font("Segoe UI Semibold", 12);
        g.DrawString("💪 Finansal Sağlık Skoru", titleFont, new SolidBrush(AppTheme.TextPrimary), 20, 15);

        if (_healthData == null)
        {
            using var loadFont = new Font("Segoe UI", 10);
            g.DrawString("Yükleniyor...", loadFont, new SolidBrush(AppTheme.TextSecondary), 20, 50);
            return;
        }

        // Skor dairesi
        var centerX = Width / 2;
        var centerY = 110;
        var radius = 55;

        // Arka plan dairesi
        using var bgPen = new Pen(Color.FromArgb(55, 65, 81), 10);
        g.DrawArc(bgPen, centerX - radius, centerY - radius, radius * 2, radius * 2, 0, 360);

        // Skor çizgisi
        var gradeColor = ColorTranslator.FromHtml(_healthData.GradeColor);
        using var scorePen = new Pen(gradeColor, 10);
        scorePen.StartCap = LineCap.Round;
        scorePen.EndCap = LineCap.Round;
        var sweepAngle = (int)(_animatedScore / 100.0 * 360);
        g.DrawArc(scorePen, centerX - radius, centerY - radius, radius * 2, radius * 2, -90, sweepAngle);

        // Skor metni
        using var scoreFont = new Font("Segoe UI", 28, FontStyle.Bold);
        var scoreText = _animatedScore.ToString();
        var scoreSize = g.MeasureString(scoreText, scoreFont);
        g.DrawString(scoreText, scoreFont, new SolidBrush(gradeColor),
            centerX - scoreSize.Width / 2, centerY - scoreSize.Height / 2 - 5);

        // Not
        using var gradeFont = new Font("Segoe UI Semibold", 12);
        var gradeSize = g.MeasureString(_healthData.Grade, gradeFont);
        g.DrawString(_healthData.Grade, gradeFont, new SolidBrush(gradeColor),
            centerX - gradeSize.Width / 2, centerY + 30);

        // Açıklama
        using var descFont = new Font("Segoe UI", 9);
        var descSize = g.MeasureString(_healthData.GradeDescription, descFont);
        g.DrawString(_healthData.GradeDescription, descFont, new SolidBrush(AppTheme.TextSecondary),
            centerX - descSize.Width / 2, Height - 35);
    }

    private GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

