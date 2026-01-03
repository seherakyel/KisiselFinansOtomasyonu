using System.IO;

namespace KisiselFinans.UI.Theme;

/// <summary>
/// Tema Yöneticisi - Dark/Light tema geçişi
/// </summary>
public static class ThemeManager
{
    public enum ThemeMode { Dark, Light }
    
    private static ThemeMode _currentTheme = ThemeMode.Dark;
    public static ThemeMode CurrentTheme => _currentTheme;
    
    public static event Action<ThemeMode>? ThemeChanged;

    // Dark Theme Colors
    public static class Dark
    {
        public static readonly Color PrimaryDark = Color.FromArgb(10, 10, 15);
        public static readonly Color PrimaryMedium = Color.FromArgb(18, 18, 25);
        public static readonly Color PrimaryLight = Color.FromArgb(28, 28, 38);
        public static readonly Color Surface = Color.FromArgb(38, 38, 52);
        public static readonly Color CardBg = Color.FromArgb(22, 22, 32);
        public static readonly Color InputBg = Color.FromArgb(15, 15, 22);
        public static readonly Color InputBorder = Color.FromArgb(55, 55, 75);
        public static readonly Color TextPrimary = Color.FromArgb(250, 250, 255);
        public static readonly Color TextSecondary = Color.FromArgb(160, 160, 185);
        public static readonly Color TextMuted = Color.FromArgb(100, 100, 130);
    }

    // Light Theme Colors
    public static class Light
    {
        public static readonly Color PrimaryDark = Color.FromArgb(245, 247, 250);
        public static readonly Color PrimaryMedium = Color.FromArgb(255, 255, 255);
        public static readonly Color PrimaryLight = Color.FromArgb(235, 238, 245);
        public static readonly Color Surface = Color.FromArgb(225, 228, 235);
        public static readonly Color CardBg = Color.FromArgb(255, 255, 255);
        public static readonly Color InputBg = Color.FromArgb(245, 247, 250);
        public static readonly Color InputBorder = Color.FromArgb(200, 205, 215);
        public static readonly Color TextPrimary = Color.FromArgb(30, 30, 45);
        public static readonly Color TextSecondary = Color.FromArgb(80, 85, 100);
        public static readonly Color TextMuted = Color.FromArgb(130, 135, 150);
    }

    // Current theme colors (dynamic)
    public static Color PrimaryDark => _currentTheme == ThemeMode.Dark ? Dark.PrimaryDark : Light.PrimaryDark;
    public static Color PrimaryMedium => _currentTheme == ThemeMode.Dark ? Dark.PrimaryMedium : Light.PrimaryMedium;
    public static Color PrimaryLight => _currentTheme == ThemeMode.Dark ? Dark.PrimaryLight : Light.PrimaryLight;
    public static Color Surface => _currentTheme == ThemeMode.Dark ? Dark.Surface : Light.Surface;
    public static Color CardBg => _currentTheme == ThemeMode.Dark ? Dark.CardBg : Light.CardBg;
    public static Color InputBg => _currentTheme == ThemeMode.Dark ? Dark.InputBg : Light.InputBg;
    public static Color InputBorder => _currentTheme == ThemeMode.Dark ? Dark.InputBorder : Light.InputBorder;
    public static Color TextPrimary => _currentTheme == ThemeMode.Dark ? Dark.TextPrimary : Light.TextPrimary;
    public static Color TextSecondary => _currentTheme == ThemeMode.Dark ? Dark.TextSecondary : Light.TextSecondary;
    public static Color TextMuted => _currentTheme == ThemeMode.Dark ? Dark.TextMuted : Light.TextMuted;

    /// <summary>
    /// Temayı değiştirir
    /// </summary>
    public static void ToggleTheme()
    {
        _currentTheme = _currentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        SaveThemePreference();
        ThemeChanged?.Invoke(_currentTheme);
    }

    /// <summary>
    /// Belirli bir temaya geçer
    /// </summary>
    public static void SetTheme(ThemeMode mode)
    {
        if (_currentTheme != mode)
        {
            _currentTheme = mode;
            SaveThemePreference();
            ThemeChanged?.Invoke(_currentTheme);
        }
    }

    /// <summary>
    /// Tema tercihini yükler
    /// </summary>
    public static void LoadThemePreference()
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.txt");
            if (File.Exists(settingsPath))
            {
                var theme = File.ReadAllText(settingsPath).Trim();
                _currentTheme = theme == "Light" ? ThemeMode.Light : ThemeMode.Dark;
            }
        }
        catch { /* Varsayılan dark tema kullan */ }
    }

    /// <summary>
    /// Tema tercihini kaydeder
    /// </summary>
    private static void SaveThemePreference()
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.txt");
            File.WriteAllText(settingsPath, _currentTheme.ToString());
        }
        catch { }
    }

    /// <summary>
    /// Kontrole tema uygular
    /// </summary>
    public static void ApplyTheme(Control control)
    {
        control.BackColor = PrimaryDark;
        control.ForeColor = TextPrimary;
        ApplyThemeRecursive(control);
    }

    private static void ApplyThemeRecursive(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            switch (ctrl)
            {
                case Button btn:
                    if (btn.BackColor == AppTheme.AccentGreen || btn.BackColor == AppTheme.AccentRed ||
                        btn.BackColor == AppTheme.AccentBlue || btn.BackColor == AppTheme.AccentPurple)
                    {
                        // Accent renkleri değiştirme
                    }
                    else
                    {
                        btn.BackColor = Surface;
                        btn.ForeColor = TextPrimary;
                    }
                    break;
                case TextBox txt:
                    txt.BackColor = InputBg;
                    txt.ForeColor = TextPrimary;
                    break;
                case ComboBox cmb:
                    cmb.BackColor = InputBg;
                    cmb.ForeColor = TextPrimary;
                    break;
                case Panel pnl:
                    if (pnl.BackColor != Color.Transparent)
                        pnl.BackColor = PrimaryMedium;
                    break;
                case Label lbl:
                    if (lbl.ForeColor != AppTheme.AccentGreen && lbl.ForeColor != AppTheme.AccentRed &&
                        lbl.ForeColor != AppTheme.AccentBlue && lbl.ForeColor != AppTheme.AccentPurple)
                    {
                        lbl.ForeColor = TextPrimary;
                    }
                    break;
                case DataGridView dgv:
                    dgv.BackgroundColor = PrimaryMedium;
                    dgv.DefaultCellStyle.BackColor = PrimaryMedium;
                    dgv.DefaultCellStyle.ForeColor = TextPrimary;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryDark;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
                    break;
            }

            if (ctrl.HasChildren)
                ApplyThemeRecursive(ctrl);
        }
    }
}

