using System.Drawing.Drawing2D;
using KisiselFinans.Business.Services;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Canlı Piyasa Verileri Kontrolü - Döviz ve Altın
/// </summary>
public class MarketDataControl : UserControl
{
    private readonly ExchangeRateService _service;
    private FlowLayoutPanel _mainLayout = null!;
    private System.Windows.Forms.Timer _refreshTimer = null!;
    private Label _lastUpdateLabel = null!;

    public MarketDataControl()
    {
        _service = new ExchangeRateService();
        InitializeComponent();
        _ = LoadDataAsync();
        StartAutoRefresh();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.PrimaryDark;
        AutoScroll = true;

        // Header
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = AppTheme.PrimaryMedium
        };

        var lblTitle = new Label
        {
            Text = "📊 Canlı Piyasa Verileri",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(20, 15),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Döviz kurları ve altın fiyatları • Kaynak: Bigpara",
            Font = new Font("Segoe UI", 10),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(22, 50),
            AutoSize = true
        };

        _lastUpdateLabel = new Label
        {
            Text = "Güncelleniyor...",
            Font = new Font("Segoe UI", 9),
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var btnRefresh = new Button
        {
            Text = "🔄 Yenile",
            Size = new Size(100, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentBlue,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (s, e) => await LoadDataAsync();

        header.Resize += (s, e) =>
        {
            btnRefresh.Location = new Point(header.Width - 120, 20);
            _lastUpdateLabel.Location = new Point(header.Width - 220, 60);
        };

        header.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnRefresh, _lastUpdateLabel });

        // Main Layout
        _mainLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(15)
        };

        var loadingLabel = new Label
        {
            Text = "⏳ Piyasa verileri yükleniyor...",
            Font = new Font("Segoe UI", 14),
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Padding = new Padding(20)
        };
        _mainLayout.Controls.Add(loadingLabel);

        Controls.Add(_mainLayout);
        Controls.Add(header);
    }

    private void StartAutoRefresh()
    {
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 60000 }; // 1 dakikada bir
        _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
        _refreshTimer.Start();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _lastUpdateLabel.Text = "Güncelleniyor...";
            _lastUpdateLabel.ForeColor = AppTheme.AccentYellow;

            var exchangeTask = _service.GetExchangeRatesAsync();
            var goldTask = _service.GetGoldPricesAsync();

            await Task.WhenAll(exchangeTask, goldTask);

            var exchangeRates = await exchangeTask;
            var goldPrices = await goldTask;

            BeginInvoke(() =>
            {
                BuildUI(exchangeRates, goldPrices);
                
                var isOffline = exchangeRates.Any(r => r.IsOffline) || goldPrices.Any(g => g.IsOffline);
                if (isOffline)
                {
                    _lastUpdateLabel.Text = $"⚠️ Çevrimdışı veri ({DateTime.Now:HH:mm})";
                    _lastUpdateLabel.ForeColor = AppTheme.AccentOrange;
                }
                else
                {
                    _lastUpdateLabel.Text = $"✓ Canlı: {DateTime.Now:HH:mm:ss}";
                    _lastUpdateLabel.ForeColor = AppTheme.AccentGreen;
                }
            });
        }
        catch (Exception ex)
        {
            BeginInvoke(() =>
            {
                _lastUpdateLabel.Text = $"❌ Hata: {ex.Message}";
                _lastUpdateLabel.ForeColor = AppTheme.AccentRed;
            });
        }
    }

    private void BuildUI(List<ExchangeRateDto> rates, List<GoldPriceDto> gold)
    {
        _mainLayout.Controls.Clear();

        // Döviz Bölümü
        AddSectionHeader("💱 DÖVİZ KURLARI");

        foreach (var rate in rates.Take(8))
        {
            AddExchangeCard(rate);
        }

        // Altın Bölümü
        AddSectionHeader("🥇 ALTIN FİYATLARI");

        foreach (var price in gold.Take(6))
        {
            AddGoldCard(price);
        }
    }

    private void AddSectionHeader(string title)
    {
        var header = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 14),
            ForeColor = AppTheme.AccentPurple,
            Size = new Size(_mainLayout.Width - 40, 45),
            Padding = new Padding(5, 15, 0, 5)
        };
        _mainLayout.Controls.Add(header);
        _mainLayout.SetFlowBreak(header, true);
    }

    private void AddExchangeCard(ExchangeRateDto rate)
    {
        var card = new Panel
        {
            Size = new Size(280, 120),
            Margin = new Padding(8),
            BackColor = AppTheme.PrimaryMedium,
            Cursor = Cursors.Hand
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Sol renk çubuğu
            var color = rate.IsPositive ? AppTheme.AccentGreen : AppTheme.AccentRed;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, 0, 0, 4, card.Height);

            // Alt border
            using var pen = new Pen(Color.FromArgb(50, 255, 255, 255));
            e.Graphics.DrawLine(pen, 0, card.Height - 1, card.Width, card.Height - 1);
        };

        // İkon (bayrak emoji)
        var icon = GetCurrencyIcon(rate.Name);
        var lblIcon = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI Emoji", 24),
            Location = new Point(15, 15),
            AutoSize = true
        };

        // Para birimi adı
        var lblName = new Label
        {
            Text = rate.Name,
            Font = new Font("Segoe UI Semibold", 12),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(60, 15),
            AutoSize = true
        };

        // Değişim yüzdesi
        var changeColor = rate.IsPositive ? AppTheme.AccentGreen : AppTheme.AccentRed;
        var changeIcon = rate.IsPositive ? "▲" : "▼";
        var lblChange = new Label
        {
            Text = $"{changeIcon} %{Math.Abs(rate.ChangePercent):F2}",
            Font = new Font("Segoe UI", 10),
            ForeColor = changeColor,
            Location = new Point(60, 38),
            AutoSize = true
        };

        // Alış fiyatı
        var lblBuyLabel = new Label
        {
            Text = "ALIŞ",
            Font = new Font("Segoe UI", 8),
            ForeColor = AppTheme.TextMuted,
            Location = new Point(15, 70),
            AutoSize = true
        };

        var lblBuy = new Label
        {
            Text = $"₺{rate.BuyRate:N4}",
            Font = new Font("Segoe UI Semibold", 14),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(15, 85),
            AutoSize = true
        };

        // Satış fiyatı
        var lblSellLabel = new Label
        {
            Text = "SATIŞ",
            Font = new Font("Segoe UI", 8),
            ForeColor = AppTheme.TextMuted,
            Location = new Point(150, 70),
            AutoSize = true
        };

        var lblSell = new Label
        {
            Text = $"₺{rate.SellRate:N4}",
            Font = new Font("Segoe UI Semibold", 14),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(150, 85),
            AutoSize = true
        };

        // Hover efekti
        card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(45, 55, 72);
        card.MouseLeave += (s, e) => card.BackColor = AppTheme.PrimaryMedium;

        card.Controls.AddRange(new Control[] { lblIcon, lblName, lblChange, lblBuyLabel, lblBuy, lblSellLabel, lblSell });
        _mainLayout.Controls.Add(card);
    }

    private void AddGoldCard(GoldPriceDto gold)
    {
        var card = new Panel
        {
            Size = new Size(220, 100),
            Margin = new Padding(8),
            BackColor = AppTheme.PrimaryMedium,
            Cursor = Cursors.Hand
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Sol altın rengi çubuğu
            using var brush = new SolidBrush(Color.FromArgb(255, 193, 7));
            e.Graphics.FillRectangle(brush, 0, 0, 4, card.Height);
        };

        // Altın ikonu
        var lblIcon = new Label
        {
            Text = "🪙",
            Font = new Font("Segoe UI Emoji", 20),
            Location = new Point(12, 12),
            AutoSize = true
        };

        // Altın adı
        var lblName = new Label
        {
            Text = gold.Name,
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(50, 15),
            AutoSize = true
        };

        // Değişim
        var changeColor = gold.IsPositive ? AppTheme.AccentGreen : AppTheme.AccentRed;
        var changeIcon = gold.IsPositive ? "▲" : "▼";
        var lblChange = new Label
        {
            Text = $"{changeIcon} %{Math.Abs(gold.ChangePercent):F2}",
            Font = new Font("Segoe UI", 9),
            ForeColor = changeColor,
            Location = new Point(50, 38),
            AutoSize = true
        };

        // Fiyat
        var lblPrice = new Label
        {
            Text = $"₺{gold.Price:N0}",
            Font = new Font("Segoe UI Semibold", 18),
            ForeColor = Color.FromArgb(255, 193, 7),
            Location = new Point(15, 60),
            AutoSize = true
        };

        card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(45, 55, 72);
        card.MouseLeave += (s, e) => card.BackColor = AppTheme.PrimaryMedium;

        card.Controls.AddRange(new Control[] { lblIcon, lblName, lblChange, lblPrice });
        _mainLayout.Controls.Add(card);
    }

    private string GetCurrencyIcon(string name)
    {
        return name.ToLower() switch
        {
            var n when n.Contains("dolar") && n.Contains("abd") => "🇺🇸",
            var n when n.Contains("euro") => "🇪🇺",
            var n when n.Contains("sterlin") => "🇬🇧",
            var n when n.Contains("frangı") || n.Contains("isviçre") => "🇨🇭",
            var n when n.Contains("yen") => "🇯🇵",
            var n when n.Contains("kanada") => "🇨🇦",
            var n when n.Contains("avustralya") => "🇦🇺",
            var n when n.Contains("ruble") => "🇷🇺",
            var n when n.Contains("riyal") => "🇸🇦",
            _ => "💵"
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}

