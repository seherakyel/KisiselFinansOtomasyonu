using System.Globalization;
using System.Text;
using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;

namespace KisiselFinans.Business.Services;

/// <summary>
/// CSV Banka Ekstresi Import Servisi ⭐
/// </summary>
public class CsvImportService
{
    private readonly IUnitOfWork _unitOfWork;
    
    // Kategori tahmini için anahtar kelimeler
    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        { "Market/Gıda", new[] { "market", "migros", "bim", "a101", "şok", "carrefour", "metro", "gıda", "bakkal" } },
        { "Ulaşım", new[] { "benzin", "opet", "shell", "bp", "petrol", "otopark", "taksi", "uber", "otobüs", "metro", "istanbulkart" } },
        { "Faturalar", new[] { "elektrik", "su", "doğalgaz", "internet", "telefon", "vodafone", "turkcell", "türk telekom", "fatura" } },
        { "Restoran/Kafe", new[] { "restoran", "restaurant", "cafe", "kahve", "starbucks", "burger", "pizza", "yemek", "döner" } },
        { "Sağlık", new[] { "eczane", "hastane", "klinik", "doktor", "sağlık", "ilaç", "pharmacy" } },
        { "Giyim", new[] { "lcw", "h&m", "zara", "mango", "defacto", "koton", "mavi", "giyim", "ayakkabı" } },
        { "Eğlence", new[] { "sinema", "netflix", "spotify", "youtube", "oyun", "steam", "playstation", "eğlence" } },
        { "Eğitim", new[] { "kitap", "kurs", "eğitim", "okul", "üniversite", "udemy", "coursera" } },
        { "Maaş", new[] { "maaş", "salary", "ücret", "havale gelen", "eft gelen" } },
        { "Ek Gelir", new[] { "kira geliri", "faiz", "temettü", "bonus", "prim" } }
    };

    public CsvImportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// CSV dosyasını parse eder ve önizleme döner
    /// </summary>
    public async Task<CsvImportResultDto> ParseCsvAsync(string filePath, int userId)
    {
        var result = new CsvImportResultDto();
        var categories = await _unitOfWork.Categories.FindAsync(c => c.UserId == null || c.UserId == userId);
        var categoryList = categories.ToList();

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            result.TotalRows = lines.Length - 1; // Header hariç

            // İlk satır header
            var header = lines[0].ToLower();
            var (dateCol, descCol, amountCol, typeCol) = DetectColumns(header);

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var columns = ParseCsvLine(lines[i]);
                    if (columns.Length < 3) continue;

                    var transaction = new ImportedTransactionDto();

                    // Tarih
                    if (DateTime.TryParse(columns[dateCol], out var date))
                        transaction.Date = date;
                    else if (DateTime.TryParseExact(columns[dateCol], new[] { "dd.MM.yyyy", "dd/MM/yyyy", "yyyy-MM-dd" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                        transaction.Date = date;
                    else
                        continue;

                    // Açıklama
                    transaction.Description = columns[descCol].Trim().Trim('"');

                    // Tutar
                    var amountStr = columns[amountCol].Replace(".", "").Replace(",", ".").Trim();
                    if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                        continue;

                    // Gelir/Gider belirleme
                    if (typeCol >= 0 && columns.Length > typeCol)
                    {
                        var typeStr = columns[typeCol].ToLower();
                        transaction.IsIncome = typeStr.Contains("gelir") || typeStr.Contains("alacak") || typeStr.Contains("+");
                    }
                    else
                    {
                        transaction.IsIncome = amount > 0;
                    }

                    transaction.Amount = Math.Abs(amount);

                    // Kategori tahmini
                    var (suggestedCategory, categoryId) = SuggestCategory(transaction.Description, transaction.IsIncome, categoryList);
                    transaction.SuggestedCategory = suggestedCategory;
                    transaction.CategoryId = categoryId;

                    result.ImportedTransactions.Add(transaction);
                    result.TotalImportedAmount += transaction.Amount;
                    result.SuccessCount++;
                }
                catch
                {
                    result.FailedCount++;
                    result.Errors.Add($"Satır {i + 1}: Parse hatası");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Dosya okuma hatası: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Parse edilmiş işlemleri veritabanına kaydeder
    /// </summary>
    public async Task<int> ImportTransactionsAsync(int userId, int accountId, List<ImportedTransactionDto> transactions)
    {
        var imported = 0;

        foreach (var tx in transactions)
        {
            // Duplicate kontrolü
            var existing = await _unitOfWork.Transactions.FirstOrDefaultAsync(t =>
                t.AccountId == accountId &&
                t.TransactionDate.Date == tx.Date.Date &&
                t.Amount == tx.Amount &&
                t.Description == tx.Description);

            if (existing != null) continue;

            var transaction = new Transaction
            {
                AccountId = accountId,
                CategoryId = tx.CategoryId,
                TransactionDate = tx.Date,
                Amount = tx.Amount,
                TransactionType = (byte)(tx.IsIncome ? 1 : 2),
                Description = tx.Description,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            imported++;
        }

        await _unitOfWork.SaveChangesAsync();
        return imported;
    }

    /// <summary>
    /// CSV header'dan kolon indekslerini belirler
    /// </summary>
    private (int dateCol, int descCol, int amountCol, int typeCol) DetectColumns(string header)
    {
        var columns = header.Split(new[] { ',', ';', '\t' });
        int dateCol = 0, descCol = 1, amountCol = 2, typeCol = -1;

        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i].ToLower().Trim();
            if (col.Contains("tarih") || col.Contains("date"))
                dateCol = i;
            else if (col.Contains("açıklama") || col.Contains("description") || col.Contains("işlem"))
                descCol = i;
            else if (col.Contains("tutar") || col.Contains("amount") || col.Contains("miktar"))
                amountCol = i;
            else if (col.Contains("tür") || col.Contains("type") || col.Contains("borç") || col.Contains("alacak"))
                typeCol = i;
        }

        return (dateCol, descCol, amountCol, typeCol);
    }

    /// <summary>
    /// CSV satırını parse eder (tırnak içindeki virgülleri koruyarak)
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if ((c == ',' || c == ';') && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result.ToArray();
    }

    /// <summary>
    /// Açıklamaya göre kategori tahmin eder
    /// </summary>
    private (string categoryName, int? categoryId) SuggestCategory(string description, bool isIncome, List<Category> categories)
    {
        var descLower = description.ToLower();

        foreach (var (categoryName, keywords) in CategoryKeywords)
        {
            if (keywords.Any(k => descLower.Contains(k)))
            {
                var category = categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase) &&
                    c.Type == (isIncome ? (byte)1 : (byte)2));

                if (category != null)
                    return (categoryName, category.Id);

                // Türü eşleşmese bile kategori adını öner
                return (categoryName, null);
            }
        }

        // Varsayılan kategori
        var defaultCategory = categories.FirstOrDefault(c =>
            c.CategoryName == "Diğer" && c.Type == (isIncome ? (byte)1 : (byte)2));

        return ("Diğer", defaultCategory?.Id);
    }
}

