using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Data.Context;

public static class DbContextFactory
{
    private static string _connectionString = string.Empty;

    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static FinansDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinansDbContext>();
        optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
        return new FinansDbContext(optionsBuilder.Options);
    }
}

