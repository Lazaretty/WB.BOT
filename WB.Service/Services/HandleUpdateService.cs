using System.IO.Compression;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using WB.Common.Enums;
using WB.DAL.Models;
using WB.DAL.Repositories;
using WB.Service.Services;
using User = WB.DAL.Models.User;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;

    private readonly UserRepository _userRepository;
    
    private readonly ChatStateRepository _chatStateRepository;
    //private readonly ILogger<HandleUpdateService> _logger;

    public HandleUpdateService(ITelegramBotClient botClient, UserRepository userRepository, ChatStateRepository chatStateRepository)//, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _chatStateRepository = chatStateRepository;
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
            "/start"   => OnStart(_botClient, message),
            _           => Usage(_botClient, message)
        };
        Message sentMessage = await action;
        //_logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handle
    }
    
    async Task<Message> OnStart(ITelegramBotClient bot, Message message)
    {
        var welcomeMessage = "Привет, этот помогает отслеживать продажи товаров на WB." +  Environment.NewLine + "Давай приступим к настройке: пришли API ключ из личного кабинет WB";

        if ((!await _userRepository.IsUserExists(message.Chat.Id)))
        {
            await _userRepository.Insert(new User()
            {
                UserChatId = message.Chat.Id,
                IsActive = true,
                ApiKey = string.Empty,
                ChatState = new ChatState()
                {
                    UserChatId = message.Chat.Id,
                    State = ChatSate.Configuration
                }
            });
        }
        return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
            text: welcomeMessage);
    }

    async Task<Message> Usage(ITelegramBotClient bot, Message message)
    {
        if(!await _userRepository.IsUserExists(message.Chat.Id))
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                text: "Введите команду /start, чтобы начать настройку");
        }
        
        var user = await _userRepository.GetAsync(message.Chat.Id);

        if (user.ChatState.State == ChatSate.Configuration)
        {
            user.ApiKey = message.Text;

            user.ChatState.State = ChatSate.Default;
            
            await _userRepository.Update(user);
        }
        await bot.SendTextMessageAsync(chatId: message.Chat.Id,
            text: "API ключ успешно сохранен");
        
        return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
            text: "Теперь вам будут проходить уведомления о продажах на WB");
    }
    
    async Task<Message> Usage2(ITelegramBotClient bot, Message message)
    {
        const string usage = "Отправьте сгенерированный отчет (в формате zip) из личного кабинета WB, чтобы получить детализацию";
        return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove());
    }
    
    #region Inline Mode

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