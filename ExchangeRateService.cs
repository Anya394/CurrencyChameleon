using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CurrencyChameleon
{
    internal class ExchangeRateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> GetExchangeRate(string currencyCode)
        {
            try
            {
                var requestUrl = "https://www.cbr-xml-daily.ru/daily_json.js";

                var response = await _httpClient.GetStringAsync(requestUrl);

                using var jsonDocument = JsonDocument.Parse(response);
                var root = jsonDocument.RootElement;

                var dateStr = root.GetProperty("Date").GetString();
                var updateDate = DateTime.Parse(dateStr);

                var valute = root.GetProperty("Valute");

                if (valute.TryGetProperty(currencyCode, out var currencyElement))
                {
                    var value = currencyElement.GetProperty("Value").GetDecimal();
                    var nominal = currencyElement.GetProperty("Nominal").GetInt32();
                    var name = currencyElement.GetProperty("Name").GetString();

                    var ratePerOne = value / nominal;

                    return await FormatCurrencyResponse(currencyCode, ratePerOne, updateDate, name);
                }

                return $"Валюта {currencyCode} не найдена. Убедитесь, что используете правильный код валюты (например, USD).";
            }
            catch (HttpRequestException ex)
            {
                return $"Ошибка сети при получении курса: {ex.Message}";
            }
            catch (JsonException ex)
            {
                return $"Ошибка обработки данных: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Неожиданная ошибка: {ex.Message}";
            }
        }

        private static async Task<string> FormatCurrencyResponse(string currencyCode, decimal rate, DateTime updateDate, string currencyName)
        {
            var currencySymbols = new Dictionary<string, string>
            {
                {"USD", "💵"}, {"EUR", "💶"}, {"GBP", "💷"}, {"JPY", "💴"},
                {"CHF", "🇨🇭"}, {"CAD", "🇨🇦"}, {"AUD", "🇦🇺"}, {"CNY", "🇨🇳"},
                {"RUB", "🇷🇺"}, {"TRY", "🇹🇷"}, {"INR", "🇮🇳"}, {"BRL", "🇧🇷"},
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
                • *1 {currencyCode}* = *{rate:F2} RUB* 🇷🇺
                • *1 RUB* 🇷🇺 = *{(1 / rate):F4} {currencyCode}*
    
                💱 *Конвертация в другие валюты:*
                🇺🇸 *1 {currencyCode}* = *{toUsd:F4} USD* 💵
                🇪🇺 *1 {currencyCode}* = *{toEur:F4} EUR* 💶
    
                ⏰ *Обновлено:* {updateDate:dd.MM.yyyy}
                🏛 *Источник:* Центральный банк РФ
                📍 *Официальные курсы валют*
            """;
        }

        private static async Task<(decimal usdRate, decimal eurRate)> GetUsdAndEurRates()
        {
            try
            {
                var requestUrl = "https://www.cbr-xml-daily.ru/daily_json.js";
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(requestUrl);

                using var jsonDocument = JsonDocument.Parse(response);
                var valute = jsonDocument.RootElement.GetProperty("Valute");

                decimal usdRate = 0;
                decimal eurRate = 0;

                if (valute.TryGetProperty("USD", out var usdElement))
                {
                    var value = usdElement.GetProperty("Value").GetDecimal();
                    var nominal = usdElement.GetProperty("Nominal").GetInt32();
                    usdRate = value / nominal;
                }

                if (valute.TryGetProperty("EUR", out var eurElement))
                {
                    var value = eurElement.GetProperty("Value").GetDecimal();
                    var nominal = eurElement.GetProperty("Nominal").GetInt32();
                    eurRate = value / nominal;
                }

                return (usdRate, eurRate);
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}
