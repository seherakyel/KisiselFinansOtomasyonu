using DevExpress.XtraEditors;
using KisiselFinans.Business.Services;
using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using KisiselFinans.Data.Repositories;

namespace KisiselFinans.UI.Forms;

public partial class CategoryDialog : XtraForm
{
    private readonly int _userId;
    private readonly int? _categoryId;
    private Category? _category;

    private TextEdit _txtName = null!;
    private ComboBoxEdit _cmbType = null!;
    private LookUpEdit _cmbParent = null!;
    private SpinEdit _txtIconIndex = null!;

    public CategoryDialog(int userId, int? categoryId)
    {
        _userId = userId;
        _categoryId = categoryId;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        Text = _categoryId.HasValue ? "Kategori Düzenle" : "Yeni Kategori";
        Size = new Size(400, 280);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var lblName = new LabelControl { Text = "Kategori Adı", Location = new Point(20, 20) };
        _txtName = new TextEdit { Location = new Point(20, 40), Size = new Size(330, 28) };

        var lblType = new LabelControl { Text = "Tür", Location = new Point(20, 75) };
        _cmbType = new ComboBoxEdit { Location = new Point(20, 95), Size = new Size(155, 28) };
        _cmbType.Properties.Items.AddRange(new[] { "Gelir", "Gider" });
        _cmbType.SelectedIndex = 1;

        var lblIcon = new LabelControl { Text = "İkon Index", Location = new Point(195, 75) };
        _txtIconIndex = new SpinEdit { Location = new Point(195, 95), Size = new Size(155, 28) };
        _txtIconIndex.Properties.MinValue = 0;
        _txtIconIndex.Properties.MaxValue = 100;

        var lblParent = new LabelControl { Text = "Üst Kategori (Opsiyonel)", Location = new Point(20, 130) };
        _cmbParent = new LookUpEdit { Location = new Point(20, 150), Size = new Size(330, 28) };
        _cmbParent.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CategoryName", "Kategori"));
        _cmbParent.Properties.DisplayMember = "CategoryName";
        _cmbParent.Properties.ValueMember = "Id";
        _cmbParent.Properties.NullText = "(Yok)";

        var btnSave = new SimpleButton
        {
            Text = "Kaydet",
            Location = new Point(170, 200),
            Size = new Size(90, 30),
            Appearance = { BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White }
        };
        btnSave.Click += async (s, e) => await SaveAsync();

        var btnCancel = new SimpleButton { Text = "İptal", Location = new Point(265, 200), Size = new Size(90, 30) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.AddRange(new Control[]
        {
            lblName, _txtName, lblType, _cmbType, lblIcon, _txtIconIndex,
            lblParent, _cmbParent, btnSave, btnCancel
        });

        Controls.Add(panel);
    }

    private async Task LoadDataAsync()
    {
        using var context = DbContextFactory.CreateContext();
        using var unitOfWork = new UnitOfWork(context);
        var service = new CategoryService(unitOfWork);

        var categories = (await service.GetUserCategoriesAsync(_userId)).Where(c => c.Id != _categoryId).ToList();
        _cmbParent.Properties.DataSource = categories;

        if (_categoryId.HasValue)
        {
            _category = await service.GetByIdAsync(_categoryId.Value);
            if (_category != null)
            {
                _txtName.Text = _category.CategoryName;
                _cmbType.SelectedIndex = _category.Type - 1;
                _txtIconIndex.Value = _category.IconIndex;
                _cmbParent.EditValue = _category.ParentId;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            XtraMessageBox.Show("Kategori adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var context = DbContextFactory.CreateContext();
            using var unitOfWork = new UnitOfWork(context);
            var service = new CategoryService(unitOfWork);

            if (_category == null)
            {
                _category = new Category { UserId = _userId };
            }
            else
            {
                _category = await service.GetByIdAsync(_categoryId!.Value);
            }

            _category!.CategoryName = _txtName.Text;
            _category.Type = (byte)(_cmbType.SelectedIndex + 1);
            _category.IconIndex = (int)_txtIconIndex.Value;
            _category.ParentId = _cmbParent.EditValue as int?;

            if (_categoryId.HasValue)
                await service.UpdateAsync(_category);
            else
                await service.CreateAsync(_category);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

