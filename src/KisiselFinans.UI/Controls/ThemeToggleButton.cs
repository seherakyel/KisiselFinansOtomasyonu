using System.Drawing.Drawing2D;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Animasyonlu Tema Değiştirme Butonu - Güneş/Ay ikonu ile
/// </summary>
public class ThemeToggleButton : UserControl
{
    private bool _isDark = true;
    private float _animationProgress = 0;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public ThemeToggleButton()
    {
        _isDark = ThemeManager.CurrentTheme == ThemeManager.ThemeMode.Dark;
        _animationProgress = _isDark ? 1 : 0;

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += AnimationTick;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Size = new Size(60, 30);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        BackColor = Color.Transparent;

        Click += OnClick;
        Paint += OnPaint;

        // Tooltip
        var tooltip = new ToolTip();
        tooltip.SetToolTip(this, "Tema Değiştir (Dark/Light)");
    }

    private void OnClick(object? sender, EventArgs e)
    {
        _isDark = !_isDark;
        _animationTimer.Start();
        ThemeManager.ToggleTheme();
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        float target = _isDark ? 1 : 0;
        float diff = target - _animationProgress;

        if (Math.Abs(diff) < 0.05f)
        {
            _animationProgress = target;
            _animationTimer.Stop();
        }
        else
        {
            _animationProgress += diff * 0.2f;
        }

        Invalidate();
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Arkaplan (pill shape)
        var bgColor = InterpolateColor(
            Color.FromArgb(135, 206, 250), // Light mode - açık mavi
            Color.FromArgb(30, 35, 50),     // Dark mode - koyu
            _animationProgress);

        using var bgBrush = new SolidBrush(bgColor);
        using var bgPath = CreatePillPath(new Rectangle(0, 0, Width - 1, Height - 1));
        g.FillPath(bgBrush, bgPath);

        // Border
        using var borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1);
        g.DrawPath(borderPen, bgPath);

        // Toggle circle position (animated)
        float circleX = 4 + _animationProgress * (Width - Height);
        int circleSize = Height - 8;

        // Daire rengi
        var circleColor = InterpolateColor(
            Color.FromArgb(255, 200, 50),  // Light - Güneş sarısı
            Color.FromArgb(200, 200, 220), // Dark - Ay gümüşü
            _animationProgress);

        using var circleBrush = new SolidBrush(circleColor);
        g.FillEllipse(circleBrush, circleX, 4, circleSize, circleSize);

        // Güneş ışınları veya Ay krateri
        if (_animationProgress < 0.5f)
        {
            // Güneş ışınları
            using var rayPen = new Pen(Color.FromArgb((int)(255 * (1 - _animationProgress * 2)), 255, 180, 0), 2);
            float cx = circleX + circleSize / 2;
            float cy = 4 + circleSize / 2;
            float rayLength = 4;

            for (int i = 0; i < 8; i++)
            {
                float angle = (float)(i * Math.PI / 4);
                float x1 = cx + (float)Math.Cos(angle) * (circleSize / 2 + 2);
                float y1 = cy + (float)Math.Sin(angle) * (circleSize / 2 + 2);
                float x2 = cx + (float)Math.Cos(angle) * (circleSize / 2 + rayLength);
                float y2 = cy + (float)Math.Sin(angle) * (circleSize / 2 + rayLength);
                g.DrawLine(rayPen, x1, y1, x2, y2);
            }
        }
        else
        {
            // Ay krateri
            using var craterBrush = new SolidBrush(Color.FromArgb((int)(100 * (_animationProgress - 0.5f) * 2), 150, 150, 170));
            g.FillEllipse(craterBrush, circleX + 5, 8, 6, 6);
            g.FillEllipse(craterBrush, circleX + 12, 12, 4, 4);
        }
    }

    private GraphicsPath CreatePillPath(Rectangle rect)
    {
        var path = new GraphicsPath();
        int radius = rect.Height;
        path.AddArc(rect.X, rect.Y, radius, radius, 90, 180);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 180);
        path.CloseFigure();
        return path;
    }

    private Color InterpolateColor(Color from, Color to, float progress)
    {
        int r = (int)(from.R + (to.R - from.R) * progress);
        int g = (int)(from.G + (to.G - from.G) * progress);
        int b = (int)(from.B + (to.B - from.B) * progress);
        return Color.FromArgb(r, g, b);
    }

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

