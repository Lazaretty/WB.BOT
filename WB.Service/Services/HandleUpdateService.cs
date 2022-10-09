using System.IO.Compression;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using WB.Service.Services;
using File = System.IO.File;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    //private readonly ILogger<HandleUpdateService> _logger;

    public HandleUpdateService(ITelegramBotClient botClient)//, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        //_logger = logger;
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message            => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage      => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery      => BotOnCallbackQueryReceived(update.CallbackQuery!),
            UpdateType.InlineQuery        => BotOnInlineQueryReceived(update.InlineQuery!),
            UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
            _                             => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        #pragma warning disable CA1031
        catch (Exception exception)
        #pragma warning restore CA1031
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        //_logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Type != MessageType.Text)
        {
            //return;
            if (message.Document is not null)
            {
                var file = await _botClient.GetFileAsync(message.Document.FileId);
                var ms = new MemoryStream();
                await _botClient.DownloadFileAsync(file.FilePath, ms);
                
                using(var zip = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    foreach(var entry in zip.Entries)
                    {
                        using(var stream = entry.Open())
                        {
                            var parser = new DataParser(stream, 0);
                            parser.ReadAndCalculate();

                            var resulrFile = parser.GenerateReportFromResultDataIncomeByArticulToStream();

                            var resultMessage = parser.GenerateReportIncomeByArticul();

                            resulrFile.Position = 0;

                            await _botClient.SendTextMessageAsync(chatId: message.Chat.Id, resultMessage);
                            
                            await _botClient.SendDocumentAsync(chatId: message.Chat.Id, new InputOnlineFile(resulrFile, "result.xls"));
                        }
                    }
                }

                ms.Close();
                ms.Dispose();
            }
        }

        var action = message.Text!.Split(' ')[0] switch
        {
            "/inline"   => SendInlineKeyboard(_botClient, message),
            "/keyboard" => SendReplyKeyboard(_botClient, message),
            "/remove"   => RemoveKeyboard(_botClient, message),
            "/photo"    => SendFile(_botClient, message),
            "/request"  => RequestContactAndLocation(_botClient, message),
            "/calc"     => Calculate(_botClient, message),
            _           => Usage(_botClient, message)
        };
        Message sentMessage = await action;
        //_logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        static async Task<Message> SendInlineKeyboard(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            // Simulate longer running task
            await Task.Delay(500);

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    },
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Choose",
                                                  replyMarkup: inlineKeyboard);
        }

        static async Task<Message> SendReplyKeyboard(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                })
                {
                    ResizeKeyboard = true
                };

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Choose",
                                                  replyMarkup: replyKeyboardMarkup);
        }

        static async Task<Message> RemoveKeyboard(ITelegramBotClient bot, Message message)
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Removing keyboard",
                                                  replyMarkup: new ReplyKeyboardRemove());
        }

        static async Task<Message> SendFile(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadDocument);
            
            string FileName = @"C:\Users\skvor\Desktop\0.xlsx";
        
            var parser = new DataParser(FileName, 0);
            parser.ReadAndCalculate();

            var file = parser.GenerateReportFromResultDataIncomeByArticulToStream();

            file.Position = 0;
            
            return await bot.SendDocumentAsync(chatId: message.Chat.Id,
                                            document: new InputOnlineFile(file, "result.xls"),
                                            caption: "Nice Picture");
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup RequestReplyKeyboard = new(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Who or Where are you?",
                                                  replyMarkup: RequestReplyKeyboard);
        }

        static async Task<Message> Calculate(ITelegramBotClient bot, Message message)
        {
            string FileName = @"C:\Users\skvor\Desktop\0.xlsx";
        
            var parser = new DataParser(FileName, 0);
            parser.ReadAndCalculate();

            var file = parser.GenerateReportFromResultDataIncomeByArticulToStream();

            file.Position = 0;
            
            return await bot.SendDocumentAsync(chatId: message.Chat.Id, new InputOnlineFile(file, "output.xls"));
        }

        static async Task<Message> Usage(ITelegramBotClient bot, Message message)
        {
            const string usage = "Usage:\n" +
                                 "/calc     - calculate report";

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: usage,
                                                  replyMarkup: new ReplyKeyboardRemove());
        }
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}");

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}");
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
    {
        //_logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "3",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent(
                    "hello"
                )
            )
        };

        await _botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
                                                results: results,
                                                isPersonal: true,
                                                cacheTime: 0);
    }

    private Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
    {
        //_logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        return Task.CompletedTask;
    }

    #endregion

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        //_logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        //_logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }
}