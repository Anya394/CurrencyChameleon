using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

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
            await SafeEditMessage(text, Keyboards.GetCurrencyKeyboard());
        }

        public async Task WithKeyboard(string text, InlineKeyboardMarkup replyMarkup)
        {
            await SafeEditMessage(text, replyMarkup);
        }

        public async Task WithTextOnly(string text)
        {
            await SafeEditMessage(text, null);
        }

        private async Task SafeEditMessage(string text, InlineKeyboardMarkup? replyMarkup)
        {
            try
            {
                await _botClient.EditMessageTextAsync(
                    chatId: _chatId,
                    messageId: _messageId,
                    text: text,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: replyMarkup,
                    cancellationToken: _cancellationToken);

                FileLogger.Info($"Message edited - " +
                    $"Chat: {_chatId}, " +
                    $"Message: {_messageId}, " +
                    $"Text: {text.Substring(0, Math.Min(50, text.Length))}...");
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified"))
            {
                Console.WriteLine($"Message not modified (chatId: {_chatId}, messageId: {_messageId})");
                FileLogger.Debug($"Message not modified - Chat: {_chatId}, Message: {_messageId}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"Failed to edit message - Chat: {_chatId}, Message: {_messageId}", ex);
            }
        }
    }
}