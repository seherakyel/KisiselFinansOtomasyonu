using KisiselFinans.Data.Context;
using KisiselFinans.UI.Forms;
using KisiselFinans.UI.Theme;
using Microsoft.Extensions.Configuration;

namespace KisiselFinans.UI;

internal static class Program
{
    public static IConfiguration Configuration { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show($"Thread Hatası:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show($"Kritik Hata:\n{ex.Message}\n\n{ex.StackTrace}", 
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            DbContextFactory.Initialize(connectionString!);

            // Tema tercihini yükle
            ThemeManager.LoadThemePreference();

            using var loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(loginForm.CurrentUser!));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Başlatma Hatası:\n{ex.Message}\n\nDetay:\n{ex.InnerException?.Message}\n\n{ex.StackTrace}", 
                "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
