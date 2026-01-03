using System.Drawing.Drawing2D;

namespace KisiselFinans.UI.Theme;

/// <summary>
/// Tüm diyaloglar için ortak stil bileşenleri - Dinamik tema desteği
/// </summary>
public static class DialogStyles
{
    // Dinamik renkler - ThemeManager'dan alınır
    public static Color BgDark => ThemeManager.PrimaryDark;
    public static Color BgCard => ThemeManager.CardBg;
    public static Color BgInput => ThemeManager.InputBg;
    public static Color BgInputFocus => ThemeManager.CurrentTheme == ThemeManager.ThemeMode.Dark 
        ? Color.FromArgb(38, 45, 55) 
        : Color.FromArgb(230, 235, 245);
    public static Color BorderDefault => ThemeManager.InputBorder;
    public static Color BorderFocus => AppTheme.AccentBlue;
    public static Color TextWhite => ThemeManager.TextPrimary;
    public static Color TextMuted => ThemeManager.TextMuted;
    public static Color TextLabel => ThemeManager.TextSecondary;

    // Accent Colors - Her iki temada aynı
    public static readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
    public static readonly Color AccentGreen = Color.FromArgb(63, 185, 132);
    public static readonly Color AccentRed = Color.FromArgb(248, 81, 73);
    public static readonly Color AccentOrange = Color.FromArgb(255, 166, 87);
    public static readonly Color AccentPurple = Color.FromArgb(163, 113, 247);

    /// <summary>
    /// Dialog form'u stilize eder
    /// </summary>
    public static void ApplyDialogStyle(Form form, int width, int height)
    {
        form.FormBorderStyle = FormBorderStyle.None;
        form.StartPosition = FormStartPosition.CenterParent;
        form.BackColor = BgDark;
        form.Size = new Size(width, height);

        // Gölge efekti için border
        form.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderDefault, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, form.Width - 1, form.Height - 1);
        };
    }

    /// <summary>
    /// Header panel oluşturur (ikon, başlık, alt başlık, kapatma butonu)
    /// </summary>
    public static Panel CreateHeader(string icon, string title, string subtitle, Color accentColor, Action onClose)
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = BgDark,
            Padding = new Padding(24, 16, 24, 16)
        };

        // Icon container with gradient
        var iconContainer = new Panel
        {
            Size = new Size(48, 48),
            Location = new Point(24, 16),
            BackColor = Color.Transparent
        };
        iconContainer.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRect(new Rectangle(0, 0, 47, 47), 12);
            using var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 48, 48),
                accentColor,
                Color.FromArgb(accentColor.R / 2, accentColor.G / 2, accentColor.B / 2),
                45f);
            e.Graphics.FillPath(brush, path);
            
            using var font = new Font("Segoe UI", 18);
            var size = e.Graphics.MeasureString(icon, font);
            e.Graphics.DrawString(icon, font, Brushes.White,
                (48 - size.Width) / 2, (48 - size.Height) / 2);
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 16),
            ForeColor = TextWhite,
            Location = new Point(84, 14),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var lblSubtitle = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted,
            Location = new Point(86, 40),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var btnClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 12),
            ForeColor = TextMuted,
            Size = new Size(32, 32),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = AccentRed; btnClose.BackColor = Color.FromArgb(40, 248, 81, 73); };
        btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = TextMuted; btnClose.BackColor = Color.Transparent; };
        btnClose.Click += (s, e) => onClose();

        header.Resize += (s, e) => btnClose.Location = new Point(header.Width - 56, 16);

        header.Controls.AddRange(new Control[] { iconContainer, lblTitle, lblSubtitle, btnClose });
        return header;
    }

    /// <summary>
    /// Footer panel oluşturur (vazgeç ve kaydet butonları)
    /// </summary>
    public static Panel CreateFooter(string saveText, Color saveColor, Action onCancel, Action onSave)
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = BgCard,
            Padding = new Padding(24, 16, 24, 16)
        };

        var btnCancel = CreateButton("Vazgeç", BorderDefault, TextWhite, false);
        btnCancel.Click += (s, e) => onCancel();

        var btnSave = CreateButton(saveText, saveColor, Color.White, true);
        btnSave.Click += (s, e) => onSave();

        footer.Resize += (s, e) =>
        {
            btnCancel.Location = new Point(footer.Width - 280, 16);
            btnSave.Location = new Point(footer.Width - 148, 16);
        };

        footer.Controls.AddRange(new Control[] { btnCancel, btnSave });
        return footer;
    }

    /// <summary>
    /// Stilize edilmiş buton oluşturur
    /// </summary>
    public static Button CreateButton(string text, Color bgColor, Color textColor, bool isPrimary)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(isPrimary ? 124 : 108, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = bgColor,
            ForeColor = textColor,
            Font = new Font("Segoe UI Semibold", 10),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = isPrimary ? 0 : 1;
        btn.FlatAppearance.BorderColor = BorderDefault;

        if (isPrimary)
        {
            var hoverColor = Color.FromArgb(
                Math.Min(bgColor.R + 20, 255),
                Math.Min(bgColor.G + 20, 255),
                Math.Min(bgColor.B + 20, 255));
            btn.MouseEnter += (s, e) => btn.BackColor = hoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = bgColor;
        }
        else
        {
            btn.MouseEnter += (s, e) => btn.BackColor = BgInput;
            btn.MouseLeave += (s, e) => btn.BackColor = bgColor;
        }

        return btn;
    }

    /// <summary>
    /// Label oluşturur
    /// </summary>
    public static Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text.ToUpper(),
            Font = new Font("Segoe UI Semibold", 9),
            ForeColor = TextLabel,
            Location = new Point(x, y),
            AutoSize = true,
            BackColor = Color.Transparent
        };
    }

    /// <summary>
    /// Stilize edilmiş TextBox oluşturur
    /// </summary>
    public static TextBox CreateTextBox(int x, int y, int width = 0)
    {
        var txt = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width > 0 ? width : 200, 36),
            Font = new Font("Segoe UI", 11),
            BackColor = BgInput,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle
        };
        txt.GotFocus += (s, e) => txt.BackColor = BgInputFocus;
        txt.LostFocus += (s, e) => txt.BackColor = BgInput;
        return txt;
    }

    /// <summary>
    /// Stilize edilmiş ComboBox oluşturur
    /// </summary>
    public static ComboBox CreateComboBox(int x, int y, int width = 0)
    {
        var cmb = new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width > 0 ? width : 200, 36),
            Font = new Font("Segoe UI", 11),
            BackColor = BgInput,
            ForeColor = TextWhite,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        return cmb;
    }

    /// <summary>
    /// Stilize edilmiş DateTimePicker oluşturur
    /// </summary>
    public static DateTimePicker CreateDatePicker(int x, int y, int width = 0)
    {
        return new DateTimePicker
        {
            Location = new Point(x, y),
            Size = new Size(width > 0 ? width : 200, 36),
            Font = new Font("Segoe UI", 11),
            Format = DateTimePickerFormat.Short,
            CalendarMonthBackground = BgCard,
            CalendarForeColor = TextWhite
        };
    }

    /// <summary>
    /// Para girişi için özel panel oluşturur
    /// </summary>
    public static (Panel container, TextBox textBox) CreateCurrencyInput(int x, int y, int width, Color accentColor)
    {
        var container = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, 48),
            BackColor = BgInput
        };
        container.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderDefault, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, container.Width - 1, container.Height - 1);
        };

        var lblCurrency = new Label
        {
            Text = "₺",
            Font = new Font("Segoe UI Semibold", 16),
            ForeColor = accentColor,
            Size = new Size(44, 48),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var divider = new Panel
        {
            Location = new Point(44, 8),
            Size = new Size(1, 32),
            BackColor = BorderDefault
        };

        var txt = new TextBox
        {
            Location = new Point(52, 10),
            Size = new Size(width - 60, 28),
            Font = new Font("Segoe UI Semibold", 14),
            BackColor = BgInput,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.None,
            Text = "0,00",
            TextAlign = HorizontalAlignment.Right
        };

        txt.GotFocus += (s, e) =>
        {
            if (txt.Text == "0,00") txt.Text = "";
            container.BackColor = BgInputFocus;
            txt.BackColor = BgInputFocus;
        };
        txt.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = "0,00";
            container.BackColor = BgInput;
            txt.BackColor = BgInput;
        };
        txt.KeyPress += (s, e) =>
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.' && e.KeyChar != (char)Keys.Back)
                e.Handled = true;
        };

        container.Controls.AddRange(new Control[] { lblCurrency, divider, txt });
        return (container, txt);
    }

    /// <summary>
    /// İçerik paneli oluşturur
    /// </summary>
    public static Panel CreateContentPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(24, 20, 24, 20),
            AutoScroll = true
        };
    }

    /// <summary>
    /// Ayırıcı çizgi oluşturur
    /// </summary>
    public static Panel CreateDivider()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = BorderDefault
        };
    }

    /// <summary>
    /// Rounded rectangle path oluşturur
    /// </summary>
    public static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}
