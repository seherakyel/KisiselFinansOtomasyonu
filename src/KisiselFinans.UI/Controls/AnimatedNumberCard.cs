using System.Drawing.Drawing2D;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Animasyonlu sayı gösteren kart - Değer yukarı doğru sayarak gelir
/// </summary>
public class AnimatedNumberCard : UserControl
{
    private decimal _targetValue;
    private decimal _currentValue;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly string _title;
    private readonly string _icon;
    private readonly Color _accentColor;
    private readonly bool _isCurrency;
    private int _animationStep = 0;
    private const int TotalSteps = 30;

    private Label _lblValue = null!;
    private Label _lblTitle = null!;
    private Label _lblIcon = null!;

    public AnimatedNumberCard(string title, string icon, Color accentColor, bool isCurrency = true)
    {
        _title = title;
        _icon = icon;
        _accentColor = accentColor;
        _isCurrency = isCurrency;
        _animationTimer = new System.Windows.Forms.Timer { Interval = 20 };
        _animationTimer.Tick += AnimationTick;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Size = new Size(280, 120);
        BackColor = Color.FromArgb(30, 35, 50);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;

        Paint += OnPaint;

        // İkon
        _lblIcon = new Label
        {
            Text = _icon,
            Font = new Font("Segoe UI Emoji", 28),
            ForeColor = _accentColor,
            Location = new Point(20, 20),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // Başlık
        _lblTitle = new Label
        {
            Text = _title.ToUpper(),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = AppTheme.TextMuted,
            Location = new Point(75, 20),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        // Değer
        _lblValue = new Label
        {
            Text = _isCurrency ? "₺ 0" : "0",
            Font = new Font("Segoe UI Semibold", 26),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 60),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        Controls.AddRange(new Control[] { _lblIcon, _lblTitle, _lblValue });

        // Hover efekti
        MouseEnter += (s, e) => { BackColor = Color.FromArgb(40, 45, 65); Invalidate(); };
        MouseLeave += (s, e) => { BackColor = Color.FromArgb(30, 35, 50); Invalidate(); };
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Sol accent çizgisi
        using var accentBrush = new SolidBrush(_accentColor);
        g.FillRectangle(accentBrush, 0, 0, 5, Height);

        // Alt glow efekti
        using var glowBrush = new LinearGradientBrush(
            new Rectangle(0, Height - 3, Width, 3),
            Color.FromArgb(50, _accentColor),
            Color.Transparent,
            LinearGradientMode.Vertical);
        g.FillRectangle(glowBrush, 0, Height - 3, Width, 3);

        // Köşe yuvarlatma için clip
        using var path = CreateRoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        using var pen = new Pen(Color.FromArgb(50, _accentColor), 1);
        g.DrawPath(pen, path);
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

    /// <summary>
    /// Değeri animasyonlu olarak günceller
    /// </summary>
    public void SetValue(decimal value)
    {
        _targetValue = value;
        _currentValue = 0;
        _animationStep = 0;
        _animationTimer.Start();
    }

    /// <summary>
    /// Değeri anında günceller (animasyonsuz)
    /// </summary>
    public void SetValueInstant(decimal value)
    {
        _targetValue = value;
        _currentValue = value;
        UpdateLabel();
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        _animationStep++;

        // Easing fonksiyonu (ease-out)
        double progress = (double)_animationStep / TotalSteps;
        double easedProgress = 1 - Math.Pow(1 - progress, 3); // Cubic ease-out

        _currentValue = (decimal)(easedProgress * (double)_targetValue);

        UpdateLabel();

        if (_animationStep >= TotalSteps)
        {
            _animationTimer.Stop();
            _currentValue = _targetValue;
            UpdateLabel();
        }
    }

    private void UpdateLabel()
    {
        if (_isCurrency)
        {
            _lblValue.Text = $"₺ {_currentValue:N2}";
        }
        else
        {
            _lblValue.Text = $"{_currentValue:N0}";
        }

        // Değere göre renk
        if (_title.Contains("Gider") || _title.Contains("Expense"))
        {
            _lblValue.ForeColor = _currentValue > 0 ? AppTheme.AccentRed : AppTheme.TextPrimary;
        }
        else if (_title.Contains("Gelir") || _title.Contains("Income"))
        {
            _lblValue.ForeColor = _currentValue > 0 ? AppTheme.AccentGreen : AppTheme.TextPrimary;
        }
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

