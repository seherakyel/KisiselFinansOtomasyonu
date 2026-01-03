using System.IO;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KisiselFinans.Business.Services;

/// <summary>
/// PDF Rapor Oluşturma Servisi
/// </summary>
public class PdfReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public PdfReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Aylık finansal rapor PDF'i oluşturur
    /// </summary>
    public async Task<byte[]> GenerateMonthlyReportAsync(int userId, int year, int month)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var accounts = await _unitOfWork.Accounts.FindAsync(a => a.UserId == userId && a.IsActive);
        var accountIds = accounts.Select(a => a.Id).ToList();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = await _unitOfWork.Transactions.FindAsync(t =>
            accountIds.Contains(t.AccountId) &&
            t.TransactionDate >= startDate &&
            t.TransactionDate <= endDate);

        var categories = await _unitOfWork.Categories.FindAsync(c => c.UserId == null || c.UserId == userId);

        // Özet hesapla
        var totalIncome = transactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.TransactionType == 2).Sum(t => t.Amount);
        var netBalance = totalIncome - totalExpense;

        // Kategori bazlı harcamalar
        var categorySpending = transactions
            .Where(t => t.TransactionType == 2)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategorySpendingItem
            {
                CategoryId = g.Key,
                CategoryName = categories.FirstOrDefault(c => c.Id == g.Key)?.CategoryName ?? "Diğer",
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var txList = transactions.ToList();
        var catList = categories.ToList();
        var accList = accounts.ToList();

        // PDF Oluştur
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, user, year, month));
                page.Content().Element(c => ComposeContent(c, totalIncome, totalExpense, netBalance, categorySpending, txList, catList, accList));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, User? user, int year, int month)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("KİŞİSEL FİNANS RAPORU")
                    .Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                column.Item().Text(monthName.ToUpper())
                    .FontSize(14).FontColor(Colors.Grey.Darken1);
                column.Item().Text($"Hazırlayan: {user?.FullName ?? user?.Username}")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        });

        container.PaddingBottom(10).BorderBottom(2).BorderColor(Colors.Blue.Darken2);
    }

    private void ComposeContent(IContainer container, decimal income, decimal expense, decimal net,
        List<CategorySpendingItem> categorySpending, List<Transaction> transactions, List<Category> categories, List<Account> accounts)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Özet Kartları
            column.Item().PaddingBottom(15).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Green.Lighten2)
                    .Background(Colors.Green.Lighten4).Padding(10)
                    .Column(c =>
                    {
                        c.Item().Text("TOPLAM GELİR").FontSize(9).FontColor(Colors.Green.Darken3);
                        c.Item().Text($"₺ {income:N2}").Bold().FontSize(16).FontColor(Colors.Green.Darken2);
                    });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(Colors.Red.Lighten2)
                    .Background(Colors.Red.Lighten4).Padding(10)
                    .Column(c =>
                    {
                        c.Item().Text("TOPLAM GİDER").FontSize(9).FontColor(Colors.Red.Darken3);
                        c.Item().Text($"₺ {expense:N2}").Bold().FontSize(16).FontColor(Colors.Red.Darken2);
                    });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(net >= 0 ? Colors.Blue.Lighten2 : Colors.Orange.Lighten2)
                    .Background(net >= 0 ? Colors.Blue.Lighten4 : Colors.Orange.Lighten4).Padding(10)
                    .Column(c =>
                    {
                        c.Item().Text("NET BAKİYE").FontSize(9).FontColor(net >= 0 ? Colors.Blue.Darken3 : Colors.Orange.Darken3);
                        c.Item().Text($"₺ {net:N2}").Bold().FontSize(16).FontColor(net >= 0 ? Colors.Blue.Darken2 : Colors.Orange.Darken2);
                    });
            });

            // Hesap Bakiyeleri
            column.Item().PaddingBottom(10).Text("HESAP BAKİYELERİ").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
            column.Item().PaddingBottom(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hesap Adı").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tür").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Bakiye").Bold();
                });

                foreach (var account in accounts)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(account.AccountName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(account.AccountType?.TypeName ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                        .Text($"₺ {account.CurrentBalance:N2}")
                        .FontColor(account.CurrentBalance >= 0 ? Colors.Green.Darken1 : Colors.Red.Darken1);
                }
            });

            // Kategori Dağılımı
            column.Item().PaddingBottom(10).Text("KATEGORİ BAZLI HARCAMALAR").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
            column.Item().PaddingBottom(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Kategori").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("İşlem").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Tutar").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Oran").Bold();
                });

                foreach (var cat in categorySpending)
                {
                    var percentage = expense > 0 ? (cat.Total / expense) * 100 : 0;
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cat.CategoryName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cat.Count.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₺ {cat.Total:N2}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"%{percentage:F1}");
                }
            });

            // Son İşlemler
            column.Item().PaddingBottom(10).Text("SON İŞLEMLER").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tarih").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Açıklama").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Kategori").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Tutar").Bold();
                });

                foreach (var tx in transactions.OrderByDescending(t => t.TransactionDate).Take(20))
                {
                    var category = categories.FirstOrDefault(c => c.Id == tx.CategoryId);
                    var isIncome = tx.TransactionType == 1;

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(tx.TransactionDate.ToString("dd.MM"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(tx.Description ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(category?.CategoryName ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                        .Text($"{(isIncome ? "+" : "-")}₺ {tx.Amount:N2}")
                        .FontColor(isIncome ? Colors.Green.Darken1 : Colors.Red.Darken1);
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Sayfa ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    /// <summary>
    /// PDF'i dosyaya kaydeder
    /// </summary>
    public async Task SaveReportAsync(int userId, int year, int month, string filePath)
    {
        var pdfBytes = await GenerateMonthlyReportAsync(userId, year, month);
        await File.WriteAllBytesAsync(filePath, pdfBytes);
    }

    private class CategorySpendingItem
    {
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal Total { get; set; }
        public int Count { get; set; }
    }
}
