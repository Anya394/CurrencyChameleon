using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CurrencyChameleon
{
    internal class Program
    {
        private static TelegramBotClient? _botClient;
        private static readonly string TelegramBotToken;

        // Поле для хранения состояния пользователя
        private static Dictionary<long, UserState> _userStates = new Dictionary<long, UserState>();

        public enum UserState
        {
            Default,
            AwaitingCurrencyCode
        }

        static Program()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<Program>();
            var configuration = builder.Build();

            TelegramBotToken = configuration["TelegramBot:Token"]
                ?? throw new InvalidOperationException("TelegramBot:Token not found in secrets");
        }

        public static async Task Main()
        {
            _botClient = new TelegramBotClient(TelegramBotToken);

            // Настройки получения обновлений
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>(), // Получать все типы обновлений
                ThrowPendingUpdates = true, // Игнорировать обновления, полученные пока бот был оффлайн
            };

            // Запускаем прием обновлений
            using var cts = new CancellationTokenSource();

            // Создаем экземпляр обработчика
            var updateHandler = new DefaultUpdateHandler(HandleUpdateAsync, HandlePollingErrorAsync);

            _botClient.StartReceiving(
                updateHandler: updateHandler,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            // Получаем информацию о боте и выводим ее в консоль
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Бот @{me.Username} запущен и ожидает сообщений!");
            Console.ReadLine(); // Бесконечно ждем, чтобы бот не закрылся

            // Отправляем сигнал отмены для остановки получения обновлений
            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Обрабатываем callback-запросы от кнопок
            if (update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQuery(botClient, callbackQuery, cancellationToken);
                return;
            }

            // Обрабатываем только текстовые сообщения
            if (update.Message is not { } message || message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            var userName = message.From?.FirstName ?? "Пользователь";

            Console.WriteLine($"{userName} ({chatId}) написал: '{messageText}'");

            // Проверяем, ожидаем ли мы ввод кода валюты от пользователя
            if (_userStates.ContainsKey(chatId) && _userStates[chatId] == UserState.AwaitingCurrencyCode)
            {
                await HandleCurrencyCodeInput(botClient, message, messageText, cancellationToken);
                return;
            }

            await ProcessMessage(botClient, chatId, messageText, cancellationToken);
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Обрабатываем ошибки API Telegram
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private static async Task ProcessMessage(ITelegramBotClient botClient, long chatId, 
            string messageText, CancellationToken cancellationToken)
        {
            string responseText;

            // Обработка команд (начинающихся с /)
            if (messageText.StartsWith("/"))
            {
                responseText = ProcessCommand(messageText);

                if (messageText.StartsWith("/rate"))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: responseText,
                        replyMarkup: Keyboards.GetCurrencyKeyboard(),
                        cancellationToken: cancellationToken);
                    return;
                }

                if (messageText.StartsWith("/start"))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: responseText,
                        replyMarkup: Keyboards.GetStartKeyboard(),
                        cancellationToken: cancellationToken);
                    return;
                }
            }
            else
            {
                responseText = ProcessTextMessage(messageText);
            }

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responseText,
                cancellationToken: cancellationToken);
        }

        private static string ProcessCommand(string command)
        {
            var commandParts = command.Split(' ');
            var mainCommand = commandParts[0].ToLower();

            return mainCommand switch
            {
                "/start" => "Добро пожаловать! Узнавайте курсы любых валют в любое время.",
                "/help" => "Доступные команды:\n/start - начать работу\n/help - показать помощь\n/about - о боте\n/rate - узнать курс валюты",
                "/about" => "Я создан на C# с использованием библиотеки Telegram.Bot. Моя цель - помогать пользователям!",
                "/rate" => "Используйте кнопки ниже для выбора валюты:",
                _ => "Неизвестная команда. Используйте /help для списка доступных команд.",
            };
        }

        private static string ProcessTextMessage(string messageText)
        {
            var lowerText = messageText.ToLower();

            return lowerText switch
            {
                string t when t.Contains("привет") || t.Contains("здравствуй") => "Привет!",
                string t when t.Contains("как дела") || t.Contains("как ты") => "У меня всё отлично! А у вас?",
                string t when t.Contains("спасибо") => "Пожалуйста! Обращайтесь ещё!",
                _ => "Извините, я не совсем понимаю. Попробуйте использовать одну из команд: /help"
            };
        }

        // Обработка нажатия кнопок
        private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, 
            CancellationToken cancellationToken)
        {
            var callbackData = callbackQuery.Data;
            var chatId = callbackQuery.Message!.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var editor = new MessageEditor(botClient, chatId, messageId, cancellationToken);

            if (callbackData!.StartsWith("currency_"))
            {
                var currencyCode = callbackData.Split('_')[1].ToUpper();

                // Показываем индикатор загрузки
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "Получаем курс...",
                    cancellationToken: cancellationToken);

                var exchangeRate = await ExchangeRateService.GetExchangeRate(currencyCode);
                await editor.WithCurrencyKeyboard(exchangeRate);
            }

            if (callbackData.Equals("currencies_more"))
            {
                // Устанавливаем состояние ожидания ввода валюты
                _userStates[chatId] = UserState.AwaitingCurrencyCode;

                // Запрашиваем у пользователя ввод кода валюты
                await HandleCustomCurrencyInput(botClient, callbackQuery, cancellationToken);
            }

            if (callbackData.Equals("cancel_input"))
            {
                await HandleCancelInput(botClient, callbackQuery, cancellationToken);
            }

            if (callbackData.Equals("find_out_course_input"))
            {
                await editor.WithCurrencyKeyboard();
            }

            if (callbackData.Equals("go_main_menu"))
            {
                var text = "Добро пожаловать! Узнавайте курсы любых валют в любое время.:";
                await editor.WithKeyboard(text, Keyboards.GetStartKeyboard());
            }
        }

        // Обработка выбора ввода кастомного кода валюты
        private static async Task HandleCustomCurrencyInput(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var message = callbackQuery.Message;
            var editor = new MessageEditor(botClient, message!.Chat.Id, message.MessageId, cancellationToken);
            await editor.WithKeyboard("💎 *Отправьте код валюты в чат*", Keyboards.GetInputCancelKeyboard());
        }

        // Обработка введённого кода валюты
        private static async Task HandleCurrencyCodeInput(ITelegramBotClient botClient, Message message, string currencyCode, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var normalizedCurrencyCode = currencyCode.Trim().ToUpper();

            _userStates.Remove(chatId);

            // Проверяем валидность кода валюты
            if (string.IsNullOrWhiteSpace(normalizedCurrencyCode) || normalizedCurrencyCode.Length != 3)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "*Неверный формат кода валюты*\n\n" +
                          "Код валюты должен состоять из 3 букв (например: USD).\n" +
                          "Попробуйте ещё раз.",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: Keyboards.GetCurrencyKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            };

            var exchangeRate = await ExchangeRateService.GetExchangeRate(normalizedCurrencyCode);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: exchangeRate,
                parseMode: ParseMode.Markdown,
                replyMarkup: Keyboards.GetCurrencyKeyboard(),
                cancellationToken: cancellationToken);
        }

        // Обработка отмены ввода
        private static async Task HandleCancelInput(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var editor = new MessageEditor(botClient, chatId, messageId, cancellationToken);

            _userStates.Remove(chatId);

            await editor.WithCurrencyKeyboard();

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "Ввод отменен",
                cancellationToken: cancellationToken);
        }
    }
}
