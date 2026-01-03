using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace KisiselFinans.Business.Services;

/// <summary>
/// Döviz ve Altın Kuru Servisi - Bigpara'dan veri çeker
/// </summary>
public class ExchangeRateService
{
    private readonly HttpClient _httpClient;
    private const string DOVIZ_URL = "https://bigpara.hurriyet.com.tr/doviz/";
    private const string ALTIN_URL = "https://bigpara.hurriyet.com.tr/altin/";

    public ExchangeRateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Döviz kurlarını çeker
    /// </summary>
    public async Task<List<ExchangeRateDto>> GetExchangeRatesAsync()
    {
        var rates = new List<ExchangeRateDto>();

        try
        {
            var html = await _httpClient.GetStringAsync(DOVIZ_URL);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Bigpara tablo yapısı: <ul class="tBody"> içindeki <li> elemanları
            var tableBody = doc.DocumentNode.SelectNodes("//ul[contains(@class,'tBody')]/li");
            
            if (tableBody != null && tableBody.Any())
            {
                foreach (var row in tableBody)
                {
                    try
                    {
                        // Döviz adı
                        var nameNode = row.SelectSingleNode(".//span[@class='hisse-name']") 
                                    ?? row.SelectSingleNode(".//h3")
                                    ?? row.SelectSingleNode(".//a");
                        
                        // Değerler
                        var valueNodes = row.SelectNodes(".//span[contains(@class,'value')]")
                                      ?? row.SelectNodes(".//li[contains(@class,'cell')]");

                        if (nameNode != null && valueNodes != null && valueNodes.Count >= 2)
                        {
                            var name = CleanText(nameNode.InnerText);
                            var buyRate = ParseDecimal(valueNodes[0]?.InnerText);
                            var sellRate = ParseDecimal(valueNodes[1]?.InnerText);
                            
                            // Değişim yüzdesi
                            decimal changePercent = 0;
                            var changeNode = row.SelectSingleNode(".//span[contains(@class,'statu')]")
                                          ?? row.SelectSingleNode(".//span[contains(@class,'rate')]");
                            if (changeNode != null)
                            {
                                changePercent = ParseDecimal(changeNode.InnerText);
                            }

                            if (!string.IsNullOrEmpty(name) && buyRate > 0)
                            {
                                rates.Add(new ExchangeRateDto
                                {
                                    Name = name,
                                    BuyRate = buyRate,
                                    SellRate = sellRate > 0 ? sellRate : buyRate,
                                    ChangePercent = changePercent,
                                    IsPositive = !changeNode?.InnerText?.Contains("-") ?? true,
                                    LastUpdate = DateTime.Now
                                });
                            }
                        }
                    }
                    catch { /* Tek satır hatası, devam et */ }
                }
            }

            // Alternatif: Farklı selector dene
            if (!rates.Any())
            {
                var altRows = doc.DocumentNode.SelectNodes("//div[contains(@class,'kurlar')]//tr")
                           ?? doc.DocumentNode.SelectNodes("//table//tbody//tr");

                if (altRows != null)
                {
                    foreach (var row in altRows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells != null && cells.Count >= 3)
                        {
                            var name = CleanText(cells[0].InnerText);
                            var buy = ParseDecimal(cells[1].InnerText);
                            var sell = ParseDecimal(cells[2].InnerText);
                            var change = cells.Count > 3 ? ParseDecimal(cells[3].InnerText) : 0;

                            if (!string.IsNullOrEmpty(name) && buy > 0)
                            {
                                rates.Add(new ExchangeRateDto
                                {
                                    Name = name,
                                    BuyRate = buy,
                                    SellRate = sell > 0 ? sell : buy,
                                    ChangePercent = change,
                                    IsPositive = change >= 0,
                                    LastUpdate = DateTime.Now
                                });
                            }
                        }
                    }
                }
            }

            // Hala boşsa, regex ile dene
            if (!rates.Any())
            {
                rates = ParseWithRegex(html);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Döviz verisi çekme hatası: {ex.Message}");
        }

        // Son çare: API dene veya fallback
        if (!rates.Any())
        {
            rates = await TryAlternativeSourceAsync() ?? GetFallbackRates();
        }

        return rates;
    }

    /// <summary>
    /// Altın fiyatlarını çeker
    /// </summary>
    public async Task<List<GoldPriceDto>> GetGoldPricesAsync()
    {
        var prices = new List<GoldPriceDto>();

        try
        {
            var html = await _httpClient.GetStringAsync(ALTIN_URL);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tableBody = doc.DocumentNode.SelectNodes("//ul[contains(@class,'tBody')]/li");

            if (tableBody != null)
            {
                foreach (var row in tableBody)
                {
                    try
                    {
                        var nameNode = row.SelectSingleNode(".//span[@class='hisse-name']")
                                    ?? row.SelectSingleNode(".//h3")
                                    ?? row.SelectSingleNode(".//a");

                        var valueNodes = row.SelectNodes(".//span[contains(@class,'value')]")
                                      ?? row.SelectNodes(".//li[contains(@class,'cell')]");

                        if (nameNode != null && valueNodes != null && valueNodes.Count >= 1)
                        {
                            var name = CleanText(nameNode.InnerText);
                            var price = ParseDecimal(valueNodes[0]?.InnerText);

                            decimal changePercent = 0;
                            var changeNode = row.SelectSingleNode(".//span[contains(@class,'statu')]");
                            if (changeNode != null)
                            {
                                changePercent = ParseDecimal(changeNode.InnerText);
                            }

                            if (!string.IsNullOrEmpty(name) && price > 0)
                            {
                                prices.Add(new GoldPriceDto
                                {
                                    Name = name,
                                    Price = price,
                                    ChangePercent = changePercent,
                                    IsPositive = !changeNode?.InnerText?.Contains("-") ?? true,
                                    LastUpdate = DateTime.Now
                                });
                            }
                        }
                    }
                    catch { }
                }
            }

            // Regex fallback
            if (!prices.Any())
            {
                prices = ParseGoldWithRegex(html);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Altın verisi çekme hatası: {ex.Message}");
        }

        if (!prices.Any())
        {
            prices = GetFallbackGoldPrices();
        }

        return prices;
    }

    private List<ExchangeRateDto> ParseWithRegex(string html)
    {
        var rates = new List<ExchangeRateDto>();

        try
        {
            // Dolar için özel regex
            var dolarMatch = Regex.Match(html, @"ABD\s*Dolar[ıi].*?(\d+[,\.]\d+).*?(\d+[,\.]\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (dolarMatch.Success)
            {
                rates.Add(new ExchangeRateDto
                {
                    Name = "ABD Doları",
                    BuyRate = ParseDecimal(dolarMatch.Groups[1].Value),
                    SellRate = ParseDecimal(dolarMatch.Groups[2].Value),
                    LastUpdate = DateTime.Now
                });
            }

            // Euro
            var euroMatch = Regex.Match(html, @"Euro.*?(\d+[,\.]\d+).*?(\d+[,\.]\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (euroMatch.Success)
            {
                rates.Add(new ExchangeRateDto
                {
                    Name = "Euro",
                    BuyRate = ParseDecimal(euroMatch.Groups[1].Value),
                    SellRate = ParseDecimal(euroMatch.Groups[2].Value),
                    LastUpdate = DateTime.Now
                });
            }

            // Sterlin
            var sterlinMatch = Regex.Match(html, @"[İI]ngiliz\s*Sterlin.*?(\d+[,\.]\d+).*?(\d+[,\.]\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (sterlinMatch.Success)
            {
                rates.Add(new ExchangeRateDto
                {
                    Name = "İngiliz Sterlini",
                    BuyRate = ParseDecimal(sterlinMatch.Groups[1].Value),
                    SellRate = ParseDecimal(sterlinMatch.Groups[2].Value),
                    LastUpdate = DateTime.Now
                });
            }
        }
        catch { }

        return rates;
    }

    private List<GoldPriceDto> ParseGoldWithRegex(string html)
    {
        var prices = new List<GoldPriceDto>();

        try
        {
            // Gram Altın
            var gramMatch = Regex.Match(html, @"Gram\s*Alt[ıi]n.*?(\d+[,\.]\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (gramMatch.Success)
            {
                prices.Add(new GoldPriceDto
                {
                    Name = "Gram Altın",
                    Price = ParseDecimal(gramMatch.Groups[1].Value),
                    LastUpdate = DateTime.Now
                });
            }

            // Çeyrek
            var ceyrekMatch = Regex.Match(html, @"[ÇC]eyrek\s*Alt[ıi]n.*?(\d+[,\.]+\d*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (ceyrekMatch.Success)
            {
                prices.Add(new GoldPriceDto
                {
                    Name = "Çeyrek Altın",
                    Price = ParseDecimal(ceyrekMatch.Groups[1].Value),
                    LastUpdate = DateTime.Now
                });
            }
        }
        catch { }

        return prices;
    }

    private async Task<List<ExchangeRateDto>?> TryAlternativeSourceAsync()
    {
        try
        {
            // Truncgil API (ücretsiz)
            var response = await _httpClient.GetStringAsync("https://api.genelpara.com/embed/doviz.json");
            var rates = new List<ExchangeRateDto>();

            // Basit JSON parse
            if (response.Contains("USD"))
            {
                var usdBuy = ExtractJsonValue(response, "USD", "satis");
                var usdSell = ExtractJsonValue(response, "USD", "alis");
                var usdChange = ExtractJsonValue(response, "USD", "degisim");

                if (usdBuy > 0)
                {
                    rates.Add(new ExchangeRateDto
                    {
                        Name = "ABD Doları",
                        BuyRate = usdBuy,
                        SellRate = usdSell > 0 ? usdSell : usdBuy,
                        ChangePercent = usdChange,
                        IsPositive = usdChange >= 0,
                        LastUpdate = DateTime.Now
                    });
                }
            }

            if (response.Contains("EUR"))
            {
                var eurBuy = ExtractJsonValue(response, "EUR", "satis");
                var eurSell = ExtractJsonValue(response, "EUR", "alis");
                var eurChange = ExtractJsonValue(response, "EUR", "degisim");

                if (eurBuy > 0)
                {
                    rates.Add(new ExchangeRateDto
                    {
                        Name = "Euro",
                        BuyRate = eurBuy,
                        SellRate = eurSell > 0 ? eurSell : eurBuy,
                        ChangePercent = eurChange,
                        IsPositive = eurChange >= 0,
                        LastUpdate = DateTime.Now
                    });
                }
            }

            if (response.Contains("GBP"))
            {
                var gbpBuy = ExtractJsonValue(response, "GBP", "satis");
                rates.Add(new ExchangeRateDto
                {
                    Name = "İngiliz Sterlini",
                    BuyRate = gbpBuy,
                    SellRate = gbpBuy,
                    LastUpdate = DateTime.Now
                });
            }

            return rates.Any() ? rates : null;
        }
        catch
        {
            return null;
        }
    }

    private decimal ExtractJsonValue(string json, string currency, string field)
    {
        try
        {
            var pattern = $@"""{currency}""[^}}]*?""{field}""\s*:\s*""?([0-9,\.]+)""?";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return ParseDecimal(match.Groups[1].Value);
            }
        }
        catch { }
        return 0;
    }

    private string CleanText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }

    private decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        // HTML decode ve temizle
        text = System.Net.WebUtility.HtmlDecode(text);
        var cleaned = Regex.Replace(text, @"[^\d,.\-]", "");
        
        // Türkçe format: 43.017,50 -> 43017.50
        if (cleaned.Contains(",") && cleaned.Contains("."))
        {
            // Binlik ayracı nokta, ondalık virgül
            if (cleaned.LastIndexOf(',') > cleaned.LastIndexOf('.'))
            {
                cleaned = cleaned.Replace(".", "").Replace(",", ".");
            }
            else
            {
                cleaned = cleaned.Replace(",", "");
            }
        }
        else if (cleaned.Contains(","))
        {
            cleaned = cleaned.Replace(",", ".");
        }
        
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        
        return 0;
    }

    private List<ExchangeRateDto> GetFallbackRates()
    {
        // Güncel veriler alınamazsa yaklaşık değerler (3 Ocak 2026 verileri)
        return new List<ExchangeRateDto>
        {
            new() { Name = "ABD Doları", BuyRate = 43.02m, SellRate = 43.03m, ChangePercent = 0.17m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Euro", BuyRate = 50.43m, SellRate = 50.46m, ChangePercent = -0.22m, IsPositive = false, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "İngiliz Sterlini", BuyRate = 57.91m, SellRate = 57.97m, ChangePercent = -0.07m, IsPositive = false, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "İsviçre Frangı", BuyRate = 54.27m, SellRate = 54.31m, ChangePercent = 0.19m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Japon Yeni", BuyRate = 0.274m, SellRate = 0.275m, ChangePercent = 0m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Kanada Doları", BuyRate = 31.40m, SellRate = 31.56m, ChangePercent = 0.24m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Avustralya Doları", BuyRate = 28.87m, SellRate = 29.01m, ChangePercent = 0.63m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true }
        };
    }

    private List<GoldPriceDto> GetFallbackGoldPrices()
    {
        return new List<GoldPriceDto>
        {
            new() { Name = "Gram Altın", Price = 4250m, ChangePercent = 0.5m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Çeyrek Altın", Price = 6950m, ChangePercent = 0.4m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Yarım Altın", Price = 13900m, ChangePercent = 0.4m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "Cumhuriyet Altını", Price = 27500m, ChangePercent = 0.3m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true },
            new() { Name = "22 Ayar Bilezik", Price = 4100m, ChangePercent = 0.5m, IsPositive = true, LastUpdate = DateTime.Now, IsOffline = true }
        };
    }
}

public class ExchangeRateDto
{
    public string Name { get; set; } = "";
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public decimal ChangePercent { get; set; }
    public bool IsPositive { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsOffline { get; set; } // Çevrimdışı/yedek veri mi?
}

public class GoldPriceDto
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal ChangePercent { get; set; }
    public bool IsPositive { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsOffline { get; set; }
}
