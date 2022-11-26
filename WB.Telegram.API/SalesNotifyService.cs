﻿using System.Drawing;
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
        var wbAdapter = new WBAdapter();
        var httpClient = new HttpClient();


        var builder = new MessageBuilder();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var users = await repository.GetAllActiveAsync();

                var notifyTasks = new List<Task>();

                var count = 0;
                
                foreach (var user in users)
                {
                    var sales = await wbAdapter.GetSales(user.ApiKey, user.LastUpdate.Value);

                    if (count == 50)
                    {
                        await Task.WhenAll(notifyTasks);
                        notifyTasks.Clear();
                    }

                    notifyTasks.Add(Notify(user, sales, repository, builder, botClient));
                    count++;
                }

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