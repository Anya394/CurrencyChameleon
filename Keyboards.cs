using Telegram.Bot.Types.ReplyMarkups;

namespace CurrencyChameleon
{
    internal class Keyboards
    {
        public static InlineKeyboardMarkup GetCurrencyKeyboard()
        {
            var keyboard = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🇺🇸 USD", "currency_usd"),
                    InlineKeyboardButton.WithCallbackData("🇪🇺 EUR", "currency_eur"),
                    InlineKeyboardButton.WithCallbackData("🇬🇧 GBP", "currency_gbp")
                },
                [
                    InlineKeyboardButton.WithCallbackData("🇯🇵 JPY", "currency_jpy"),
                    InlineKeyboardButton.WithCallbackData("🇨🇭 CHF", "currency_chf"),
                    InlineKeyboardButton.WithCallbackData("🇨🇦 CAD", "currency_cad")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("🇦🇺 AUD", "currency_aud"),
                    InlineKeyboardButton.WithCallbackData("🇨🇳 CNY", "currency_cny"),
                    InlineKeyboardButton.WithCallbackData("🇨🇿 CZK", "currency_czk")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("🇹🇷 TRY", "currency_try"),
                    InlineKeyboardButton.WithCallbackData("🇧🇷 BRL", "currency_brl"),
                    InlineKeyboardButton.WithCallbackData("🇮🇳 INR", "currency_inr")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("💎 Еще валюты", "currencies_more")
                ]
            };

            return new InlineKeyboardMarkup(keyboard);
        }

        public static InlineKeyboardMarkup GetInputCancelKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Отмена", "cancel_input") }
            });
        }
    }
}
