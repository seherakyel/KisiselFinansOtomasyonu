using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;
using KisiselFinans.UI.Theme;
using System.Drawing.Drawing2D;

namespace KisiselFinans.UI.Forms;

public class CategoryDialog : Form
{
    private readonly int _userId;
    private readonly int? _categoryId;
    private Category? _category;

    private TextBox _txtName = null!;
    private Panel _typeIncomeBtn = null!;
    private Panel _typeExpenseBtn = null!;
    private byte _selectedType = 2;

    private const int DIALOG_WIDTH = 400;
    private const int DIALOG_HEIGHT = 360;
    private const int FIELD_WIDTH = 352;

    public CategoryDialog(int userId, int? categoryId = null)
    {
        _userId = userId;
        _categoryId = categoryId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        var isEdit = _categoryId.HasValue;
        DialogStyles.ApplyDialogStyle(this, DIALOG_WIDTH, DIALOG_HEIGHT);

        // Header
        var header = DialogStyles.CreateHeader(
            "🏷", isEdit ? "Kategori Düzenle" : "Yeni Kategori", "İşlem kategorisi oluşturun",
            Color.FromArgb(236, 72, 153),
            () => { DialogResult = DialogResult.Cancel; Close(); });

        // Content
        var content = DialogStyles.CreateContentPanel();

        int y = 8;

        // Kategori Adı
        content.Controls.Add(DialogStyles.CreateLabel("Kategori Adı", 0, y));
        _txtName = DialogStyles.CreateTextBox(0, y + 24, FIELD_WIDTH);
        _txtName.Font = new Font("Segoe UI", 12);
        content.Controls.Add(_txtName);
        y += 72;

        // Kategori Türü
        content.Controls.Add(DialogStyles.CreateLabel("Kategori Türü", 0, y));
        y += 28;

        int btnWidth = (FIELD_WIDTH - 12) / 2;

        // Gelir butonu
        _typeIncomeBtn = CreateTypeButton("+ Gelir", DialogStyles.AccentGreen, 0, y, btnWidth, () => SelectType(1));
        content.Controls.Add(_typeIncomeBtn);

        // Gider butonu
        _typeExpenseBtn = CreateTypeButton("− Gider", DialogStyles.AccentRed, btnWidth + 12, y, btnWidth, () => SelectType(2));
        content.Controls.Add(_typeExpenseBtn);

        SelectType(2); // Default: Gider

        // Footer
        var footer = DialogStyles.CreateFooter(
            "Kaydet",
            Color.FromArgb(236, 72, 153),
            () => { DialogResult = DialogResult.Cancel; Close(); },
            async () => await SaveAsync());

        var divider = DialogStyles.CreateDivider();

        Controls.Add(content);
        Controls.Add(divider);
        Controls.Add(header);
        Controls.Add(footer);
    }

    private Panel CreateTypeButton(string text, Color color, int x, int y, int width, Action onClick)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, 50),
            BackColor = DialogStyles.BgInput,
            Cursor = Cursors.Hand
        };
        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(DialogStyles.BorderDefault, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 12),
            ForeColor = color,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
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
            _typeIncomeBtn.BackColor = DialogStyles.AccentGreen;
            ((Label)_typeIncomeBtn.Controls[0]).ForeColor = Color.White;
            _typeExpenseBtn.BackColor = DialogStyles.BgInput;
            ((Label)_typeExpenseBtn.Controls[0]).ForeColor = DialogStyles.AccentRed;
        }
        else
        {
            _typeExpenseBtn.BackColor = DialogStyles.AccentRed;
            ((Label)_typeExpenseBtn.Controls[0]).ForeColor = Color.White;
            _typeIncomeBtn.BackColor = DialogStyles.BgInput;
            ((Label)_typeIncomeBtn.Controls[0]).ForeColor = DialogStyles.AccentGreen;
        }
    }

    private async Task LoadDataAsync()
    {
        if (_categoryId.HasValue)
        {
            try
            {
                using var context = DbContextFactory.CreateContext();
                using var unitOfWork = new UnitOfWork(context);
                var service = new CategoryService(unitOfWork);

                _category = await service.GetByIdAsync(_categoryId.Value);
                if (_category != null)
                {
                    BeginInvoke(() =>
                    {
                        _txtName.Text = _category.CategoryName;
                        SelectType(_category.Type);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Kategori adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            _category!.CategoryName = _txtName.Text.Trim();
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
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
