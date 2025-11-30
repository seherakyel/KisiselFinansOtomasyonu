using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using KisiselFinans.Data.Context;
using KisiselFinans.UI.Forms;
using Microsoft.Extensions.Configuration;

namespace KisiselFinans.UI;

internal static class Program
{
    public static IConfiguration Configuration { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Konfigürasyonu yükle
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Configuration = builder.Build();

        // Veritabanı bağlantısını başlat
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        DbContextFactory.Initialize(connectionString!);

        // DevExpress tema ayarları (Office 2019 Colorful)
        BonusSkins.Register();
        SkinManager.EnableFormSkins();
        UserLookAndFeel.Default.SetSkinStyle(SkinStyle.Office2019Colorful);

        // Login formu ile başla
        using var loginForm = new LoginForm();
        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            Application.Run(new MainRibbonForm(loginForm.CurrentUser!));
        }
    }
}

