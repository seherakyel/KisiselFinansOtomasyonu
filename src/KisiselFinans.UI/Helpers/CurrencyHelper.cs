namespace KisiselFinans.UI.Helpers;

public static class CurrencyHelper
{
    public static string FormatCurrency(decimal amount, string currencyCode = "TRY")
    {
        return currencyCode switch
        {
            "USD" => $"${amount:N2}",
            "EUR" => $"€{amount:N2}",
            "GBP" => $"£{amount:N2}",
            "XAU" => $"{amount:N4} gr",
            _ => $"{amount:N2} ₺"
        };
    }

    public static string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "XAU" => "gr",
            _ => "₺"
        };
    }
}

