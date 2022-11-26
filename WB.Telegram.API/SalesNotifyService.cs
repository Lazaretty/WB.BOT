using System.Drawing;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using WB.DAL.Models;
using WB.DAL.Repositories;
using WB.Service.Helper;
using WB.Service.Models;
using WB.Service.Services;

namespace WB.Telegram.API;

public class SalesNotifyService : BackgroundService
{
    private readonly ILogger<SalesNotifyService> _logger;
    private readonly IServiceProvider _services;
    private readonly TelegramConfiguration _botConfig;

    public SalesNotifyService(ILogger<SalesNotifyService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _services = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5_000);
        
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var repository = scope.ServiceProvider.GetRequiredService<UserRepository>();
        var wbAdapter =  scope.ServiceProvider.GetRequiredService<WBAdapter>();
        var httpClient = new HttpClient();
        
        await botClient.SendTextMessageAsync(chatId: "669363145",
            text: "Run SalesNotifyService",
            replyMarkup: new ReplyKeyboardRemove(),
            parseMode: ParseMode.Markdown);
        
        var builder = new MessageBuilder();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var users = await repository.GetAllActiveAsync();

                var notifyTasks = new List<Task>();

                var count = 0;
                
                foreach (var user in users.Where(x => !string.IsNullOrEmpty(x.ApiKey)))
                {
                    await botClient.SendTextMessageAsync(chatId: "669363145",
                        text: $"Run user {user.UserChatId}",
                        replyMarkup: new ReplyKeyboardRemove(),
                        parseMode: ParseMode.Markdown);
                    
                    var sales = await wbAdapter.GetSales(user.ApiKey, user.LastUpdate.Value);

                    if (sales == null || !sales.Any())
                    {
                        await botClient.SendTextMessageAsync(chatId: "669363145",
                            text: $"didnt fond any sales for user {user.UserChatId}, for date {user.LastUpdate.Value.ToString("dd.MM.yy HH:mm")}",
                            replyMarkup: new ReplyKeyboardRemove(),
                            parseMode: ParseMode.Markdown);
                       continue;
                    }

                    await botClient.SendTextMessageAsync(chatId: "669363145",
                        text: $"Get sale {sales.Count()} for user {user.UserChatId}",
                        replyMarkup: new ReplyKeyboardRemove(),
                        parseMode: ParseMode.Markdown);
                    
                    if (count == 50)
                    {
                        await Task.WhenAll(notifyTasks);
                        notifyTasks.Clear();
                        
                        await botClient.SendTextMessageAsync(chatId: "669363145",
                            text: $"users notified",
                            replyMarkup: new ReplyKeyboardRemove(),
                            parseMode: ParseMode.Markdown);
                    }

                    notifyTasks.Add(Notify(user, sales, repository, builder, botClient));
                    count++;
                }

                await botClient.SendTextMessageAsync(chatId: "669363145",
                    text: $"users notified. with tasks {notifyTasks.Count}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    parseMode: ParseMode.Markdown); 
                
                await Task.WhenAll(notifyTasks);

            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"[{GetType().Name}]");
            }

            await Task.Delay(30_000, stoppingToken);
        }
    }

    private async Task Notify(User user ,IEnumerable<Sale> sales, UserRepository repository, MessageBuilder builder, ITelegramBotClient botClient)
    {
        foreach (var sale in sales)
        {
            var content = new MemoryStream();
                        
            try
            {
                await builder.BuildInputOnlineFileFromSaleInfo(sale, content);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can't generate image");
            }

            content.Position = 0;

            if (content.Length > 0)
            {
                await botClient.SendPhotoAsync(
                    chatId: user.UserChatId,
                    new InputOnlineFile(content),
                    caption: sale.ToMessage(),
                    parseMode: ParseMode.Markdown);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.UserChatId,
                    text: sale.ToMessage(),
                    replyMarkup: new ReplyKeyboardRemove(),
                    parseMode: ParseMode.Markdown);
            }
        }
        
        if (sales.Any())
        {
            user.LastUpdate = DateTimeOffset.UtcNow;
            await repository.Update(user);
        }
    }
}