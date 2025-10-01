using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CurrencyChameleon
{
    public class MessageEditor(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly long _chatId = chatId;
        private readonly int _messageId = messageId;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public async Task WithCurrencyKeyboard(string text = "Используйте кнопки ниже для выбора валюты:")
        {
            await _botClient.EditMessageTextAsync(
                chatId: _chatId,
                messageId: _messageId,
                text: text,
                parseMode: ParseMode.Markdown,
                replyMarkup: Keyboards.GetCurrencyKeyboard(),
                cancellationToken: _cancellationToken);
        }

        public async Task WithKeyboard(string text, InlineKeyboardMarkup replyMarkup)
        {
            await _botClient.EditMessageTextAsync(
                chatId: _chatId,
                messageId: _messageId,
                text: text,
                parseMode: ParseMode.Markdown,
                replyMarkup: replyMarkup,
                cancellationToken: _cancellationToken);
        }

        public async Task WithTextOnly(string text)
        {
            await _botClient.EditMessageTextAsync(
                chatId: _chatId,
                messageId: _messageId,
                text: text,
                parseMode: ParseMode.Markdown,
                cancellationToken: _cancellationToken);
        }
    }
}
