namespace KisiselFinans.UI.Theme;

public static class AppTheme
{
    // === DYNAMIC THEME COLORS (ThemeManager'dan alınır) ===
    
    // Ana Arka Plan Renkleri - Dinamik
    public static Color PrimaryDark => ThemeManager.PrimaryDark;
    public static Color PrimaryMedium => ThemeManager.PrimaryMedium;
    public static Color PrimaryLight => ThemeManager.PrimaryLight;
    public static Color Surface => ThemeManager.Surface;
    
    // Metin Renkleri - Dinamik
    public static Color TextPrimary => ThemeManager.TextPrimary;
    public static Color TextSecondary => ThemeManager.TextSecondary;
    public static Color TextMuted => ThemeManager.TextMuted;
    
    // Kart ve Input Renkleri - Dinamik
    public static Color CardBg => ThemeManager.CardBg;
    public static Color CardBgHover => ThemeManager.CurrentTheme == ThemeManager.ThemeMode.Dark 
        ? Color.FromArgb(32, 32, 45) 
        : Color.FromArgb(240, 242, 248);
    public static Color InputBg => ThemeManager.InputBg;
    public static Color InputBorder => ThemeManager.InputBorder;
    public static Color InputFocus => GradientStart;

    // === STATIC ACCENT COLORS (Her iki temada aynı) ===
    
    // Gradient Renkleri
    public static readonly Color GradientStart = Color.FromArgb(79, 70, 229);    // Electric Indigo
    public static readonly Color GradientMid = Color.FromArgb(147, 51, 234);     // Vivid Purple  
    public static readonly Color GradientEnd = Color.FromArgb(236, 72, 153);     // Hot Pink

    // Vurgu Renkleri - Neon Style
    public static readonly Color AccentBlue = Color.FromArgb(56, 189, 248);      // Sky Blue
    public static readonly Color AccentGreen = Color.FromArgb(52, 211, 153);     // Emerald
    public static readonly Color AccentRed = Color.FromArgb(251, 113, 133);      // Rose
    public static readonly Color AccentOrange = Color.FromArgb(251, 146, 60);    // Orange
    public static readonly Color AccentPurple = Color.FromArgb(192, 132, 252);   // Lavender
    public static readonly Color AccentCyan = Color.FromArgb(34, 211, 238);      // Cyan
    public static readonly Color AccentPink = Color.FromArgb(244, 114, 182);     // Pink
    public static readonly Color AccentYellow = Color.FromArgb(250, 204, 21);    // Yellow

    // Özel Efektler
    public static readonly Color GlowPurple = Color.FromArgb(40, 147, 51, 234);
    public static readonly Color GlowBlue = Color.FromArgb(40, 56, 189, 248);

    // Fontlar
    public static readonly Font FontTitle = new("Segoe UI", 28F, FontStyle.Bold);
    public static readonly Font FontSubtitle = new("Segoe UI Semibold", 16F);
    public static readonly Font FontBody = new("Segoe UI", 11F);
    public static readonly Font FontSmall = new("Segoe UI", 10F);
    public static readonly Font FontButton = new("Segoe UI Semibold", 11F);
    public static readonly Font FontLabel = new("Segoe UI", 10F);

    public static void ApplyTheme(Control control)
    {
        control.BackColor = PrimaryDark;
        control.ForeColor = TextPrimary;
        control.Font = FontBody;
        ApplyThemeRecursive(control);
    }

    private static void ApplyThemeRecursive(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            ctrl.Font = FontBody;

            switch (ctrl)
            {
                case Button btn:
                    StyleButton(btn);
                    break;
                case TextBox txt:
                    StyleTextBox(txt);
                    break;
                case ComboBox cmb:
                    StyleComboBox(cmb);
                    break;
                case DataGridView dgv:
                    StyleDataGrid(dgv);
                    break;
                case Panel pnl:
                    if (pnl.BackColor != Color.Transparent)
                        pnl.BackColor = PrimaryMedium;
                    break;
                case Label lbl:
                    if (!IsAccentColor(lbl.ForeColor))
                        lbl.ForeColor = TextPrimary;
                    break;
                case NumericUpDown nud:
                    StyleNumericUpDown(nud);
                    break;
                case DateTimePicker dtp:
                    StyleDatePicker(dtp);
                    break;
                case CheckBox chk:
                    chk.ForeColor = TextPrimary;
                    break;
            }

            if (ctrl.HasChildren)
                ApplyThemeRecursive(ctrl);
        }
    }

    private static bool IsAccentColor(Color color)
    {
        return color == AccentGreen || color == AccentRed || color == AccentBlue ||
               color == AccentPurple || color == AccentOrange || color == AccentCyan ||
               color == AccentPink || color == AccentYellow;
    }

    public static void StyleButton(Button btn, bool isPrimary = false)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Font = FontButton;

        if (isPrimary)
        {
            btn.BackColor = GradientStart;
            btn.ForeColor = Color.White;
        }
        else if (!IsAccentColor(btn.BackColor))
        {
            btn.BackColor = Surface;
            btn.ForeColor = TextPrimary;
        }
    }

    public static void StyleSuccessButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = AccentGreen;
        btn.ForeColor = Color.FromArgb(20, 20, 30);
        btn.Cursor = Cursors.Hand;
        btn.Font = FontButton;
    }

    public static void StyleDangerButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = AccentRed;
        btn.ForeColor = Color.FromArgb(20, 20, 30);
        btn.Cursor = Cursors.Hand;
        btn.Font = FontButton;
    }

    public static void StyleTextBox(TextBox txt)
    {
        txt.BackColor = InputBg;
        txt.ForeColor = TextPrimary;
        txt.BorderStyle = BorderStyle.FixedSingle;
        txt.Font = FontBody;
    }

    public static void StyleComboBox(ComboBox cmb)
    {
        cmb.BackColor = InputBg;
        cmb.ForeColor = TextPrimary;
        cmb.FlatStyle = FlatStyle.Flat;
        cmb.Font = FontBody;
    }

    public static void StyleNumericUpDown(NumericUpDown nud)
    {
        nud.BackColor = InputBg;
        nud.ForeColor = TextPrimary;
        nud.BorderStyle = BorderStyle.FixedSingle;
        nud.Font = FontBody;
    }

    public static void StyleDatePicker(DateTimePicker dtp)
    {
        dtp.CalendarMonthBackground = InputBg;
        dtp.CalendarForeColor = TextPrimary;
        dtp.Font = FontBody;
    }

    public static void StyleDataGrid(DataGridView dgv)
    {
        dgv.BackgroundColor = PrimaryMedium;
        dgv.GridColor = PrimaryLight;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowHeadersVisible = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgv.DefaultCellStyle.BackColor = PrimaryMedium;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = GradientStart;
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.DefaultCellStyle.Font = FontBody;
        dgv.DefaultCellStyle.Padding = new Padding(12, 8, 12, 8);

        dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryDark;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = AccentPurple;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 10, 12, 10);
        dgv.ColumnHeadersHeight = 50;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgv.AlternatingRowsDefaultCellStyle.BackColor = PrimaryLight;
        dgv.RowTemplate.Height = 45;
    }

    public static Panel CreateCard(string title)
    {
        var card = new Panel
        {
            BackColor = CardBg,
            Padding = new Padding(20)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = FontSubtitle,
            ForeColor = AccentPurple,
            Dock = DockStyle.Top,
            Height = 35
        };

        card.Controls.Add(lblTitle);
        return card;
    }

    // Gradient çizim - 3 renkli
    public static void DrawGradientBackground(Graphics g, Rectangle rect, bool diagonal = true)
    {
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            rect, GradientStart, GradientEnd,
            diagonal ? System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal
                     : System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
        
        var blend = new System.Drawing.Drawing2D.ColorBlend(3);
        blend.Colors = new[] { GradientStart, GradientMid, GradientEnd };
        blend.Positions = new[] { 0f, 0.5f, 1f };
        brush.InterpolationColors = blend;
        
        g.FillRectangle(brush, rect);
    }

    // Gelir/Gider renkleri
    public static Color GetTransactionColor(byte type) => type switch
    {
        1 => AccentGreen,  // Gelir
        2 => AccentRed,    // Gider
        _ => AccentBlue    // Transfer
    };
}
