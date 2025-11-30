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
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Configuration = builder.Build();

        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        DbContextFactory.Initialize(connectionString!);

        using var loginForm = new LoginForm();
        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            Application.Run(new MainForm(loginForm.CurrentUser!));
        }
    }
}
