using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;

namespace KisiselFinans.UI.Forms;

public class CategoryDialog : Form
{
    private readonly int _userId;
    private readonly int? _categoryId;
    private Category? _category;

    private TextBox _txtName = null!;
    private Panel _typeIncomeBtn = null!;
    private Panel _typeExpenseBtn = null!;
    private byte _selectedType = 2; // Default: Gider

    private static readonly Color AccentColor = Color.FromArgb(236, 72, 153);
    private static readonly Color IncomeColor = Color.FromArgb(34, 197, 94);
    private static readonly Color ExpenseColor = Color.FromArgb(239, 68, 68);
    private static readonly Color BgDark = Color.FromArgb(17, 24, 39);
    private static readonly Color BgCard = Color.FromArgb(31, 41, 55);
    private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public CategoryDialog(int userId, int? categoryId)
    {
        _userId = userId;
        _categoryId = categoryId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _categoryId.HasValue;
        Text = isEdit ? "Kategori Duzenle" : "Yeni Kategori";
        Size = new Size(450, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = BgDark;

        var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };

        // ===== HEADER =====
        var header = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = BgDark };

        var iconPanel = new Panel { Size = new Size(56, 56), Location = new Point(30, 17) };
        iconPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(AccentColor);
            e.Graphics.FillEllipse(brush, 0, 0, 55, 55);
            using var font = new Font("Segoe UI", 22);
            e.Graphics.DrawString("🏷", font, Brushes.White, 8, 8);
        };

        header.Controls.Add(iconPanel);
        header.Controls.Add(new Label
        {
            Text = isEdit ? "Kategori Duzenle" : "Yeni Kategori",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(100, 22),
            AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = "Islem kategorisi olusturun",
            Font = new Font("Segoe UI", 11),
            ForeColor = TextMuted,
            Location = new Point(102, 52),
            AutoSize = true
        });
        header.Controls.Add(CreateCloseButton());

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BorderColor };

        // ===== CONTENT =====
        var content = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(30, 25, 30, 25) };

        int y = 0;

        // KATEGORI ADI
        content.Controls.Add(new Label
        {
            Text = "Kategori Adi", Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White, Location = new Point(0, y), AutoSize = true
        });

        _txtName = new TextBox
        {
            Location = new Point(0, y + 30), Size = new Size(375, 45),
            Font = new Font("Segoe UI", 13), BackColor = BgCard,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(_txtName);

        y += 90;

        // TUR SECIMI
        content.Controls.Add(new Label
        {
            Text = "Kategori Turu", Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White, Location = new Point(0, y), AutoSize = true
        });

        y += 30;

        // Gelir Button
        _typeIncomeBtn = CreateTypeButton("+ Gelir", IncomeColor, 0, y, () => SelectType(1));
        content.Controls.Add(_typeIncomeBtn);

        // Gider Button
        _typeExpenseBtn = CreateTypeButton("- Gider", ExpenseColor, 195, y, () => SelectType(2));
        SelectType(2); // Default
        content.Controls.Add(_typeExpenseBtn);

        // ===== FOOTER =====
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 85, BackColor = Color.FromArgb(24, 32, 48) };

        var btnCancel = new Button
        {
            Text = "Vazgec", Size = new Size(120, 45), Location = new Point(120, 20),
            FlatStyle = FlatStyle.Flat, BackColor = BorderColor, ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11), Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnSave = new Button
        {
            Text = "Kaydet", Size = new Size(140, 45), Location = new Point(250, 20),
            FlatStyle = FlatStyle.Flat, BackColor = AccentColor, ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11), Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveAsync();

        footer.Controls.AddRange(new Control[] { btnCancel, btnSave });

        var accentLine = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = AccentColor };

        mainPanel.Controls.Add(content);
        mainPanel.Controls.Add(divider);
        mainPanel.Controls.Add(header);
        mainPanel.Controls.Add(footer);
        mainPanel.Controls.Add(accentLine);

        Controls.Add(mainPanel);
    }

    private Panel CreateTypeButton(string text, Color color, int x, int y, Action onClick)
    {
        var panel = new Panel
        {
            Location = new Point(x, y), Size = new Size(180, 55),
            BackColor = BgCard, Cursor = Cursors.Hand
        };

        var lbl = new Label
        {
            Text = text, Font = new Font("Segoe UI Semibold", 13),
            ForeColor = color, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand
        };

        panel.Click += (s, e) => onClick();
        lbl.Click += (s, e) => onClick();

        panel.Controls.Add(lbl);
        return panel;
    }

    private void SelectType(byte type)
    {
        _selectedType = type;

        if (type == 1)
        {
            _typeIncomeBtn.BackColor = IncomeColor;
            _typeIncomeBtn.Controls[0].ForeColor = Color.White;
            _typeExpenseBtn.BackColor = BgCard;
            _typeExpenseBtn.Controls[0].ForeColor = ExpenseColor;
        }
        else
        {
            _typeExpenseBtn.BackColor = ExpenseColor;
            _typeExpenseBtn.Controls[0].ForeColor = Color.White;
            _typeIncomeBtn.BackColor = BgCard;
            _typeIncomeBtn.Controls[0].ForeColor = IncomeColor;
        }
    }

    private Label CreateCloseButton()
    {
        var btn = new Label
        {
            Text = "✕", Font = new Font("Segoe UI", 14),
            ForeColor = Color.FromArgb(100, 116, 139),
            Size = new Size(44, 44), Location = new Point(390, 10),
            TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btn.MouseEnter += (s, e) => btn.ForeColor = Color.FromArgb(239, 68, 68);
        btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(100, 116, 139);
        return btn;
    }

    private async Task LoadDataAsync()
    {
        if (_categoryId.HasValue)
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new CategoryService(unitOfWork);

            _category = await service.GetByIdAsync(_categoryId.Value);
            if (_category != null)
            {
                _txtName.Text = _category.CategoryName;
                SelectType(_category.Type);
            }
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Kategori adi zorunludur.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new CategoryService(unitOfWork);

            if (_categoryId.HasValue)
                _category = await service.GetByIdAsync(_categoryId.Value);
            else
                _category = new Category { UserId = _userId };

            _category!.CategoryName = _txtName.Text;
            _category.Type = _selectedType;
            _category.IconIndex = 0;

            if (_categoryId.HasValue)
                await service.UpdateAsync(_category);
            else
                await service.CreateAsync(_category);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayit hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
