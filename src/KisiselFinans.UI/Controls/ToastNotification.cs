using System.Drawing.Drawing2D;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Modern Toast Bildirim Bileşeni ⭐
/// </summary>
public class ToastNotification : Form
{
    private System.Windows.Forms.Timer _timer = null!;
    private int _duration;
    private double _opacity = 0;
    private bool _isClosing = false;

    public enum ToastType { Info, Success, Warning, Error }

    private ToastNotification() { }

    public static void Show(string title, string message, ToastType type = ToastType.Info, int durationMs = 4000)
    {
        var toast = new ToastNotification();
        toast.Initialize(title, message, type, durationMs);
        toast.Show();
    }

    private void Initialize(string title, string message, ToastType type, int durationMs)
    {
        _duration = durationMs;

        // Form ayarları
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(360, 90);
        BackColor = Color.FromArgb(22, 27, 34);
        Opacity = 0;

        // Konumu hesapla (sağ alt köşe)
        var workingArea = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(workingArea.Right - Width - 20, workingArea.Bottom - Height - 20);

        // Renk ve ikon belirle
        var (accentColor, icon) = type switch
        {
            ToastType.Success => (Color.FromArgb(63, 185, 132), "✓"),
            ToastType.Warning => (Color.FromArgb(255, 166, 87), "⚠"),
            ToastType.Error => (Color.FromArgb(248, 81, 73), "✕"),
            _ => (Color.FromArgb(88, 166, 255), "ℹ")
        };

        // Paint event
        Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Arka plan
            using var bgBrush = new SolidBrush(Color.FromArgb(22, 27, 34));
            e.Graphics.FillRectangle(bgBrush, ClientRectangle);

            // Sol accent çizgisi
            using var accentBrush = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);

            // İkon dairesi
            e.Graphics.FillEllipse(accentBrush, 15, 20, 44, 44);
            using var iconFont = new Font("Segoe UI", 18, FontStyle.Bold);
            var iconSize = e.Graphics.MeasureString(icon, iconFont);
            e.Graphics.DrawString(icon, iconFont, Brushes.White,
                15 + (44 - iconSize.Width) / 2,
                20 + (44 - iconSize.Height) / 2);

            // Border
            using var borderPen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        };

        // Başlık
        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.FromArgb(240, 246, 252),
            Location = new Point(70, 18),
            AutoSize = true
        };

        // Mesaj
        var lblMessage = new Label
        {
            Text = message.Length > 60 ? message[..57] + "..." : message,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(125, 133, 144),
            Location = new Point(70, 44),
            Size = new Size(260, 40),
            AutoEllipsis = true
        };

        // Kapatma butonu
        var btnClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(125, 133, 144),
            Size = new Size(24, 24),
            Location = new Point(Width - 32, 8),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => StartClose();
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(248, 81, 73);
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(125, 133, 144);

        Controls.AddRange(new Control[] { lblTitle, lblMessage, btnClose });

        // Animasyon timer
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        // Tıklanınca kapat
        Click += (s, e) => StartClose();
        lblTitle.Click += (s, e) => StartClose();
        lblMessage.Click += (s, e) => StartClose();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            _opacity -= 0.1;
            if (_opacity <= 0)
            {
                _timer.Stop();
                Close();
                Dispose();
            }
            else
            {
                Opacity = _opacity;
            }
        }
        else
        {
            if (_opacity < 1)
            {
                _opacity += 0.1;
                Opacity = Math.Min(1, _opacity);
            }
            else
            {
                _duration -= 16;
                if (_duration <= 0)
                {
                    StartClose();
                }
            }
        }
    }

    private void StartClose()
    {
        _isClosing = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW - taskbar'da görünmesin
            return cp;
        }
    }
}

/// <summary>
/// Toast gösterme yardımcı sınıfı
/// </summary>
public static class Toast
{
    public static void Info(string title, string message) =>
        ToastNotification.Show(title, message, ToastNotification.ToastType.Info);

    public static void Success(string title, string message) =>
        ToastNotification.Show(title, message, ToastNotification.ToastType.Success);

    public static void Warning(string title, string message) =>
        ToastNotification.Show(title, message, ToastNotification.ToastType.Warning);

    public static void Error(string title, string message) =>
        ToastNotification.Show(title, message, ToastNotification.ToastType.Error);

    public static void BudgetAlert(string categoryName, decimal percentage)
    {
        if (percentage >= 100)
            Error($"{categoryName} Bütçesi Aşıldı!", $"Limit aşımı: %{percentage:F0}");
        else if (percentage >= 80)
            Warning($"{categoryName} Bütçesi Dolmak Üzere!", $"Kullanılan: %{percentage:F0}");
    }
}

