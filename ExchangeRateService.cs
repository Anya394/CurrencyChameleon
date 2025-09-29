using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CurrencyChameleon
{
    internal class ExchangeRateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://openexchangerates.org/api";

        private static readonly string ApiKey;

        static ExchangeRateService()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<Program>();
            var configuration = builder.Build();

            ApiKey = configuration["OpenExchangeRates:ApiKey"]
                ?? throw new InvalidOperationException("OpenExchangeRates:ApiKey not found in secrets");
        }

        public static async Task<string> GetExchangeRate(string currencyCode)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/latest.json?app_id={ApiKey}&base=USD";

                // Отправляем запрос к API
                var response = await _httpClient.GetStringAsync(requestUrl);

                using var jsonDocument = JsonDocument.Parse(response);
                var root = jsonDocument.RootElement;

                if (!root.TryGetProperty("rates", out var ratesElement))
                {
                    return "Ошибка: не удалось получить данные о курсах валют";
                }

                // Получаем timestamp обновления данных
                var timestamp = root.GetProperty("timestamp").GetInt64();
                var updateDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

                // Ищем запрошенную валюту
                if (ratesElement.TryGetProperty(currencyCode, out var rateValue))
                {
                    var rate = rateValue.GetDecimal();
                    return await FormatCurrencyResponse(currencyCode, rate, updateDate, "USD");
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

        private static async Task<string> FormatCurrencyResponse(string currencyCode, decimal rate, DateTime updateDate, string baseCurrency = "USD")
        {
            var currencySymbols = new Dictionary<string, string>
            {
                {"USD", "💵"}, {"EUR", "💶"}, {"GBP", "💷"}, {"JPY", "💴"},
                {"RUB", "🇷🇺"}, {"CNY", "🇨🇳"}, {"CHF", "🇨🇭"}, {"CAD", "🇨🇦"},
                {"AUD", "🇦🇺"}, {"BTC", "₿"}, {"ETH", "Ξ"},
                {"TRY", "🇹🇷"}, {"INR", "🇮🇳"}, {"BRL", "🇧🇷"}, {"MXN", "🇲🇽"},
                {"KRW", "🇰🇷"}, {"SGD", "🇸🇬"}, {"NZD", "🇳🇿"}, {"SEK", "🇸🇪"},
                {"NOK", "🇳🇴"}, {"DKK", "🇩🇰"}, {"ZAR", "🇿🇦"}, {"HKD", "🇭🇰"}
            };

            var symbol = currencySymbols.ContainsKey(currencyCode) ? currencySymbols[currencyCode] : "💱";

            var baseSymbol = currencySymbols.ContainsKey(baseCurrency) ? currencySymbols[baseCurrency] : "💵";

            return $"""
            {symbol} *Курс {currencyCode}*
    
            • *1 {baseCurrency}* {baseSymbol} = *{rate:F4} {currencyCode}* {symbol}
            • *1 {currencyCode}* {symbol} = *{(1 / rate):F4} {baseCurrency}* {baseSymbol}
    
            📊 *Относительно основных валют:*
            🇺🇸 USD: {rate:F4}
            🇪🇺 EUR: {await GetConvertedRate(currencyCode, "EUR"):F4}
            🇬🇧 GBP: {await GetConvertedRate(currencyCode, "GBP"):F4}
            🇨🇭 CHF: {await GetConvertedRate(currencyCode, "CHF"):F4}
    
            ⏰ *Обновлено:* {updateDate:dd.MM.yyyy HH:mm}
            📍 *Источник:* Open Exchange Rates
            🔄 *Базовая валюта:* {baseCurrency}
            """;
        }

        public static async Task<decimal> GetConvertedRate(string fromCurrency, string toCurrency, decimal amount = 1)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/convert/{amount}/{fromCurrency}/{toCurrency}?app_id={ApiKey}";

                var response = await _httpClient.GetStringAsync(requestUrl);
                using var jsonDocument = JsonDocument.Parse(response);

                var responseElement = jsonDocument.RootElement;
                if (responseElement.TryGetProperty("response", out var responseValue))
                {
                    return responseValue.GetDecimal();
                }

                throw new Exception("Не удалось получить данные конвертации");
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
