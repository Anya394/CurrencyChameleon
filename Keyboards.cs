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
                ],
                [
                    InlineKeyboardButton.WithCallbackData("В начало", "go_main_menu")
                ]
            };

            return new InlineKeyboardMarkup(keyboard);
        }

        public static InlineKeyboardMarkup GetInputCancelKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
                [InlineKeyboardButton.WithCallbackData("Отмена", "cancel_input")]
            ]);
        }

        public static InlineKeyboardMarkup GetStartKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
                [InlineKeyboardButton.WithCallbackData("👀 Узнать курс", "find_out_course_input")]
            ]);
        }
    }
}
