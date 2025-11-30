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
    private ComboBox _cmbType = null!;
    private NumericUpDown _txtIconIndex = null!;

    public CategoryDialog(int userId, int? categoryId)
    {
        _userId = userId;
        _categoryId = categoryId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _categoryId.HasValue ? "🏷️ Kategori Düzenle" : "🏷️ Yeni Kategori";
        Size = new Size(400, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.PrimaryDark;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30),
            BackColor = AppTheme.PrimaryDark
        };

        int y = 10;
        const int spacing = 55;

        var lblName = CreateLabel("Kategori Adı", y);
        _txtName = new TextBox { Location = new Point(0, y + 20), Size = new Size(320, 32) };
        AppTheme.StyleTextBox(_txtName);

        y += spacing;
        var lblType = CreateLabel("Tür", y);
        _cmbType = new ComboBox
        {
            Location = new Point(0, y + 20),
            Size = new Size(155, 32),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbType.Items.AddRange(new[] { "Gelir", "Gider" });
        _cmbType.SelectedIndex = 1;
        AppTheme.StyleComboBox(_cmbType);

        var lblIcon = CreateLabel("İkon Index", y, 170);
        _txtIconIndex = new NumericUpDown
        {
            Location = new Point(170, y + 20),
            Size = new Size(150, 32),
            Minimum = 0,
            Maximum = 100
        };
        AppTheme.StyleNumericUpDown(_txtIconIndex);

        y += spacing + 20;
        var btnSave = new Button
        {
            Text = "💾 KAYDET",
            Location = new Point(140, y),
            Size = new Size(90, 38)
        };
        AppTheme.StyleSuccessButton(btnSave);
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new Button
        {
            Text = "İPTAL",
            Location = new Point(240, y),
            Size = new Size(80, 38)
        };
        AppTheme.StyleButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblName, _txtName, lblType, _cmbType, lblIcon, _txtIconIndex, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private Label CreateLabel(string text, int y, int x = 0) => new()
    {
        Text = text,
        Font = AppTheme.FontSmall,
        ForeColor = AppTheme.TextSecondary,
        Location = new Point(x, y),
        AutoSize = true
    };

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
                _cmbType.SelectedIndex = _category.Type - 1;
                _txtIconIndex.Value = _category.IconIndex;
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
            {
                _category = await service.GetByIdAsync(_categoryId.Value);
            }
            else
            {
                _category = new Category { UserId = _userId };
            }

            _category!.CategoryName = _txtName.Text;
            _category.Type = (byte)(_cmbType.SelectedIndex + 1);
            _category.IconIndex = (int)_txtIconIndex.Value;

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
