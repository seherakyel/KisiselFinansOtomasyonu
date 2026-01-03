using System.Drawing.Drawing2D;
using KisiselFinans.Core.DTOs;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Akıllı İçgörüler Paneli ⭐
/// </summary>
public class InsightsPanel : UserControl
{
    private List<InsightDto> _insights = new();
    private Panel _contentPanel = null!;

    public InsightsPanel()
    {
        Size = new Size(350, 300);
        BackColor = Color.Transparent;
        DoubleBuffered = true;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Başlık
        var header = new Label
        {
            Text = "💡 Akıllı İçgörüler",
            Font = new Font("Segoe UI Semibold", 12),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(15, 10),
            AutoSize = true
        };
        Controls.Add(header);

        // İçerik paneli
        _contentPanel = new Panel
        {
            Location = new Point(0, 40),
            Size = new Size(Width, Height - 45),
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        Controls.Add(_contentPanel);
    }

    public void SetInsights(List<InsightDto> insights)
    {
        _insights = insights;
        RenderInsights();
    }

    private void RenderInsights()
    {
        _contentPanel.Controls.Clear();
        var y = 5;

        foreach (var insight in _insights.Take(5))
        {
            var card = CreateInsightCard(insight, y);
            _contentPanel.Controls.Add(card);
            y += 55;
        }

        if (!_insights.Any())
        {
            var emptyLabel = new Label
            {
                Text = "🎉 Şu an için yeni içgörü yok!",
                Font = new Font("Segoe UI", 10),
                ForeColor = AppTheme.TextSecondary,
                Location = new Point(15, 20),
                AutoSize = true
            };
            _contentPanel.Controls.Add(emptyLabel);
        }
    }

    private Panel CreateInsightCard(InsightDto insight, int y)
    {
        var severityColor = insight.Severity switch
        {
            "SUCCESS" => Color.FromArgb(63, 185, 132),
            "WARNING" => Color.FromArgb(255, 166, 87),
            "ALERT" => Color.FromArgb(248, 81, 73),
            _ => Color.FromArgb(88, 166, 255)
        };

        var card = new Panel
        {
            Size = new Size(Width - 20, 50),
            Location = new Point(10, y),
            BackColor = AppTheme.CardBackground,
            Cursor = Cursors.Hand
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Sol çizgi
            using var brush = new SolidBrush(severityColor);
            e.Graphics.FillRectangle(brush, 0, 0, 3, card.Height);

            // Border
            using var pen = new Pen(Color.FromArgb(55, 65, 81));
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // İkon
        var icon = new Label
        {
            Text = insight.Icon,
            Font = new Font("Segoe UI", 14),
            Location = new Point(10, 12),
            Size = new Size(30, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(icon);

        // Başlık
        var title = new Label
        {
            Text = insight.Title,
            Font = new Font("Segoe UI Semibold", 9),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(42, 8),
            Size = new Size(card.Width - 55, 18),
            AutoEllipsis = true
        };
        card.Controls.Add(title);

        // Açıklama
        var desc = new Label
        {
            Text = insight.Description.Length > 50 ? insight.Description[..47] + "..." : insight.Description,
            Font = new Font("Segoe UI", 8),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(42, 26),
            Size = new Size(card.Width - 55, 16),
            AutoEllipsis = true
        };
        card.Controls.Add(desc);

        // Hover efekti
        card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(40, 46, 54);
        card.MouseLeave += (s, e) => card.BackColor = AppTheme.CardBackground;

        return card;
    }
}

