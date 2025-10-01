using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CurrencyChameleon
{
    internal class ExchangeRateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> GetExchangeRate(string currencyCode)
        {
            var currencyData = await GetCurrencyRateAsync(currencyCode);

            if (currencyData.HasValue)
            {
                var (rate, name, updateDate) = currencyData.Value;
                return FormatCurrencyResponse(currencyCode, rate, updateDate, name);
            }

            return $"Валюта {currencyCode} не найдена. Убедитесь, что используете правильный код валюты (например, USD).";
        }

        private static string FormatCurrencyResponse(string currencyCode, decimal rate, DateTime updateDate, string currencyName)
        {
            var currencySymbols = new Dictionary<string, string>
            {
                {"USD", "🇺🇸"}, {"EUR", "🇪🇺"}, {"GBP", "🇬🇧"}, {"JPY", "🇯🇵"},
                {"CHF", "🇨🇭"}, {"CAD", "🇨🇦"}, {"AUD", "🇦🇺"}, {"CNY", "🇨🇳"},
                {"CZK", "🇨🇿"}, {"TRY", "🇹🇷"}, {"INR", "🇮🇳"}, {"BRL", "🇧🇷"},
                {"KRW", "🇰🇷"}, {"SGD", "🇸🇬"}, {"NZD", "🇳🇿"}, {"SEK", "🇸🇪"},
                {"NOK", "🇳🇴"}, {"DKK", "🇩🇰"}, {"ZAR", "🇿🇦"}, {"HKD", "🇭🇰"},
                {"PLN", "🇵🇱"}, {"THB", "🇹🇭"}, {"UAH", "🇺🇦"}, {"KZT", "🇰🇿"},
                {"BYN", "🇧🇾"}, {"AMD", "🇦🇲"}, {"AZN", "🇦🇿"}, {"GEL", "🇬🇪"}
            };

            var symbol = currencySymbols.ContainsKey(currencyCode) ?
                        currencySymbols[currencyCode] : "💱";

            var (usdToRub, eurToRub) = GetUsdAndEurRates().Result;

            decimal toUsd = usdToRub > 0 ? rate / usdToRub : 0; // RUB/валюту ÷ RUB/USD = валюта/USD
            decimal toEur = eurToRub > 0 ? rate / eurToRub : 0; // RUB/валюту ÷ RUB/EUR = валюта/EUR

            return $"""
               {symbol} *{currencyCode} - {currencyName}*
    
                💰 *Официальный курс ЦБ РФ:*
                • *1 {currencyCode}* {symbol} = *{rate:F2} RUB* 🇷🇺
                • *1 RUB* 🇷🇺 = *{(1 / rate):F4} {currencyCode}* {symbol}
    
                💱 *Конвертация в другие валюты:*
                🇺🇸 *1 {currencyCode}* = *{toUsd:F4} USD* 🇺🇸
                🇪🇺 *1 {currencyCode}* = *{toEur:F4} EUR* 🇪🇺
    
                ⏰ *Обновлено:* {updateDate:dd.MM.yyyy}
                🏛 *Источник:* Центральный банк РФ
                📍 *Официальные курсы валют*
            """;
        }

        private static async Task<(decimal usdRate, decimal eurRate)> GetUsdAndEurRates()
        {
            var usdData = await GetCurrencyRateAsync("USD");
            var eurData = await GetCurrencyRateAsync("EUR");

            return (usdData?.rate ?? 0, eurData?.rate ?? 0);
        }

        private static async Task<(decimal rate, string name, DateTime updateDate)?> GetCurrencyRateAsync(string currencyCode)
        {
            try
            {
                var requestUrl = "https://www.cbr-xml-daily.ru/daily_json.js";
                var response = await _httpClient.GetStringAsync(requestUrl);

                using var jsonDocument = JsonDocument.Parse(response);
                var root = jsonDocument.RootElement;

                var dateStr = root.GetProperty("Date").GetString();
                var updateDate = DateTime.Parse(dateStr!);
                var valute = root.GetProperty("Valute");

                if (valute.TryGetProperty(currencyCode, out var currencyElement))
                {
                    var value = currencyElement.GetProperty("Value").GetDecimal();
                    var nominal = currencyElement.GetProperty("Nominal").GetInt32();
                    var name = currencyElement.GetProperty("Name").GetString();

                    var ratePerOne = value / nominal;

                    return (ratePerOne, name!, updateDate);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
