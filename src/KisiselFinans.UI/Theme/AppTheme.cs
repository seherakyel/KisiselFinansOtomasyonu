namespace KisiselFinans.UI.Theme;

public static class AppTheme
{
    // === PREMIUM DARK THEME ===
    
    // Ana Arka Plan Renkleri
    public static readonly Color PrimaryDark = Color.FromArgb(10, 10, 15);       // Derin siyah
    public static readonly Color PrimaryMedium = Color.FromArgb(18, 18, 25);     // Koyu lacivert
    public static readonly Color PrimaryLight = Color.FromArgb(28, 28, 38);      // Açık koyu
    public static readonly Color Surface = Color.FromArgb(38, 38, 52);           // Yüzey

    // Gradient Renkleri - Elektrik Mavisi & Neon Mor
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

    // Metin Renkleri
    public static readonly Color TextPrimary = Color.FromArgb(250, 250, 255);    // Beyaz
    public static readonly Color TextSecondary = Color.FromArgb(160, 160, 185);  // Gri-Mor
    public static readonly Color TextMuted = Color.FromArgb(100, 100, 130);      // Soluk

    // Kart ve Input Renkleri
    public static readonly Color CardBg = Color.FromArgb(22, 22, 32);
    public static readonly Color CardBgHover = Color.FromArgb(32, 32, 45);
    public static readonly Color InputBg = Color.FromArgb(15, 15, 22);
    public static readonly Color InputBorder = Color.FromArgb(55, 55, 75);
    public static readonly Color InputFocus = Color.FromArgb(79, 70, 229);

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
                    pnl.BackColor = PrimaryMedium;
                    break;
                case Label lbl:
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

    public static void StyleButton(Button btn, bool isPrimary = false)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Font = FontButton;

        if (isPrimary)
        {
            btn.BackColor = GradientStart;
            btn.ForeColor = TextPrimary;
        }
        else
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
        btn.ForeColor = PrimaryDark;
        btn.Cursor = Cursors.Hand;
        btn.Font = FontButton;
    }

    public static void StyleDangerButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = AccentRed;
        btn.ForeColor = PrimaryDark;
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
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
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
