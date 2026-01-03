using System.Drawing.Drawing2D;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Controls;

/// <summary>
/// Tasarruf Hedefleri Kartı - Progress bar ile hedefler
/// </summary>
public class SavingsGoalsCard : UserControl
{
    private readonly int _userId;
    private List<SavingsGoal> _goals = new();
    private FlowLayoutPanel _goalsPanel = null!;

    public SavingsGoalsCard(int userId)
    {
        _userId = userId;
        InitializeComponent();
        _ = LoadGoalsAsync();
    }

    private void InitializeComponent()
    {
        Size = new Size(350, 280);
        BackColor = Color.FromArgb(30, 35, 50);
        Padding = new Padding(15);

        // Başlık
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "🎯 Tasarruf Hedeflerim",
            Font = new Font("Segoe UI Semibold", 13),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 5),
            AutoSize = true
        };

        var btnAdd = new Button
        {
            Text = "+",
            Size = new Size(30, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentPurple,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        btnAdd.FlatAppearance.BorderSize = 0;
        btnAdd.Click += (s, e) => ShowAddGoalDialog();

        header.Resize += (s, e) => btnAdd.Location = new Point(header.Width - 35, 5);
        header.Controls.AddRange(new Control[] { lblTitle, btnAdd });

        // Hedefler listesi
        _goalsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        Controls.Add(_goalsPanel);
        Controls.Add(header);
    }

    private async Task LoadGoalsAsync()
    {
        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);

            // SavingsGoals tablosu henüz yoksa boş liste döndür
            try
            {
                var goals = await unitOfWork.SavingsGoals.FindAsync(g => g.UserId == _userId && g.IsActive);
                _goals = goals.OrderByDescending(g => g.ProgressPercentage).ToList();
            }
            catch
            {
                // Tablo yoksa örnek veri göster
                _goals = GetSampleGoals();
            }

            BeginInvoke(() => BuildGoalsList());
        }
        catch
        {
            _goals = GetSampleGoals();
            BeginInvoke(() => BuildGoalsList());
        }
    }

    private List<SavingsGoal> GetSampleGoals()
    {
        return new List<SavingsGoal>
        {
            new() { Name = "Yeni Telefon", TargetAmount = 45000, CurrentAmount = 32000, Icon = "📱", Color = "#6366F1" },
            new() { Name = "Tatil Fonu", TargetAmount = 25000, CurrentAmount = 8500, Icon = "✈️", Color = "#10B981" },
            new() { Name = "Acil Durum", TargetAmount = 50000, CurrentAmount = 15000, Icon = "🏦", Color = "#F59E0B" }
        };
    }

    private void BuildGoalsList()
    {
        _goalsPanel.Controls.Clear();

        if (!_goals.Any())
        {
            var lblEmpty = new Label
            {
                Text = "Henüz hedef eklenmemiş.\n+ butonuna tıklayarak ekleyin.",
                Font = new Font("Segoe UI", 10),
                ForeColor = AppTheme.TextMuted,
                Size = new Size(300, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _goalsPanel.Controls.Add(lblEmpty);
            return;
        }

        foreach (var goal in _goals.Take(4))
        {
            var goalItem = CreateGoalItem(goal);
            _goalsPanel.Controls.Add(goalItem);
        }
    }

    private Panel CreateGoalItem(SavingsGoal goal)
    {
        var panel = new Panel
        {
            Size = new Size(310, 55),
            Margin = new Padding(0, 5, 0, 5),
            BackColor = Color.FromArgb(40, 45, 60),
            Cursor = Cursors.Hand
        };

        var goalColor = ColorTranslator.FromHtml(goal.Color);

        // İkon
        var lblIcon = new Label
        {
            Text = goal.Icon,
            Font = new Font("Segoe UI Emoji", 16),
            Location = new Point(10, 8),
            AutoSize = true
        };

        // İsim
        var lblName = new Label
        {
            Text = goal.Name,
            Font = new Font("Segoe UI Semibold", 10),
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(45, 5),
            AutoSize = true
        };

        // Tutar bilgisi
        var lblAmount = new Label
        {
            Text = $"₺{goal.CurrentAmount:N0} / ₺{goal.TargetAmount:N0}",
            Font = new Font("Segoe UI", 9),
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(45, 22),
            AutoSize = true
        };

        // Yüzde
        var percentage = Math.Min(100, goal.ProgressPercentage);
        var lblPercent = new Label
        {
            Text = $"%{percentage:F0}",
            Font = new Font("Segoe UI Semibold", 10),
            ForeColor = goalColor,
            Location = new Point(260, 10),
            AutoSize = true
        };

        // Progress bar (custom)
        var progressPanel = new Panel
        {
            Location = new Point(45, 42),
            Size = new Size(220, 8),
            BackColor = Color.FromArgb(60, 65, 80)
        };

        var progressFill = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size((int)(220 * percentage / 100), 8),
            BackColor = goalColor
        };
        progressPanel.Controls.Add(progressFill);

        // Hover
        panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(50, 55, 75);
        panel.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(40, 45, 60);

        // Click - para ekle
        panel.Click += (s, e) => ShowAddMoneyDialog(goal);

        panel.Controls.AddRange(new Control[] { lblIcon, lblName, lblAmount, lblPercent, progressPanel });
        return panel;
    }

    private void ShowAddGoalDialog()
    {
        using var dialog = new Form
        {
            Text = "Yeni Hedef Ekle",
            Size = new Size(400, 350),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = AppTheme.PrimaryDark,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblName = new Label { Text = "Hedef Adı:", Location = new Point(20, 20), ForeColor = AppTheme.TextPrimary, AutoSize = true };
        var txtName = new TextBox { Location = new Point(20, 45), Size = new Size(340, 30), BackColor = AppTheme.InputBg, ForeColor = AppTheme.TextPrimary };

        var lblAmount = new Label { Text = "Hedef Tutar (₺):", Location = new Point(20, 85), ForeColor = AppTheme.TextPrimary, AutoSize = true };
        var txtAmount = new TextBox { Location = new Point(20, 110), Size = new Size(340, 30), BackColor = AppTheme.InputBg, ForeColor = AppTheme.TextPrimary };

        var lblIcon = new Label { Text = "İkon:", Location = new Point(20, 150), ForeColor = AppTheme.TextPrimary, AutoSize = true };
        var cmbIcon = new ComboBox { Location = new Point(20, 175), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbIcon.Items.AddRange(new[] { "🎯", "📱", "🚗", "🏠", "✈️", "💻", "🎮", "👗", "🏦", "💍", "🎓", "🏥" });
        cmbIcon.SelectedIndex = 0;

        var btnSave = new Button
        {
            Text = "Kaydet",
            Location = new Point(150, 250),
            Size = new Size(100, 40),
            BackColor = AppTheme.AccentGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || !decimal.TryParse(txtAmount.Text, out var amount))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var context = DbContextFactory.CreateContext();
                using var unitOfWork = new UnitOfWork(context);

                var goal = new SavingsGoal
                {
                    UserId = _userId,
                    Name = txtName.Text,
                    TargetAmount = amount,
                    Icon = cmbIcon.SelectedItem?.ToString() ?? "🎯"
                };

                await unitOfWork.SavingsGoals.AddAsync(goal);
                await unitOfWork.SaveChangesAsync();

                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
                _ = LoadGoalsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        dialog.Controls.AddRange(new Control[] { lblName, txtName, lblAmount, txtAmount, lblIcon, cmbIcon, btnSave });
        dialog.ShowDialog();
    }

    private void ShowAddMoneyDialog(SavingsGoal goal)
    {
        using var dialog = new Form
        {
            Text = $"{goal.Name} - Para Ekle",
            Size = new Size(350, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = AppTheme.PrimaryDark,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblInfo = new Label
        {
            Text = $"Mevcut: ₺{goal.CurrentAmount:N0} / ₺{goal.TargetAmount:N0}",
            Location = new Point(20, 20),
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true
        };

        var lblAmount = new Label { Text = "Eklenecek Tutar (₺):", Location = new Point(20, 55), ForeColor = AppTheme.TextPrimary, AutoSize = true };
        var txtAmount = new TextBox { Location = new Point(20, 80), Size = new Size(290, 30), BackColor = AppTheme.InputBg, ForeColor = AppTheme.TextPrimary };

        var btnAdd = new Button
        {
            Text = "Ekle",
            Location = new Point(120, 120),
            Size = new Size(100, 35),
            BackColor = AppTheme.AccentGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAdd.FlatAppearance.BorderSize = 0;
        btnAdd.Click += async (s, e) =>
        {
            if (!decimal.TryParse(txtAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Geçerli bir tutar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var context = DbContextFactory.CreateContext();
                using var unitOfWork = new UnitOfWork(context);

                var dbGoal = await unitOfWork.SavingsGoals.GetByIdAsync(goal.Id);
                if (dbGoal != null)
                {
                    dbGoal.CurrentAmount += amount;
                    if (dbGoal.CurrentAmount >= dbGoal.TargetAmount)
                    {
                        dbGoal.IsCompleted = true;
                        MessageBox.Show("🎉 Tebrikler! Hedefinize ulaştınız!", "Başarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    unitOfWork.SavingsGoals.Update(dbGoal);
                    await unitOfWork.SaveChangesAsync();
                }

                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
                _ = LoadGoalsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        dialog.Controls.AddRange(new Control[] { lblInfo, lblAmount, txtAmount, btnAdd });
        dialog.ShowDialog();
    }

    public void RefreshData() => _ = LoadGoalsAsync();
}

