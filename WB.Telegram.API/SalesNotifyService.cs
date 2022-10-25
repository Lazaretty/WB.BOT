﻿using Telegram.Bot;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WB.DAL.Repositories;
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
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var repository = scope.ServiceProvider.GetRequiredService<UserRepository>();
        var wbAdapter = new WBAdapter();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var users = await repository.GetAllActiveAsync();

                foreach (var user in users)
                {
                    var sales = await wbAdapter.GetSales(user.ApiKey);
                    foreach (var sale in sales)
                    {
                        await botClient.SendTextMessageAsync(chatId: user.UserChatId,
                            text: sale.ToString(),
                            replyMarkup: new ReplyKeyboardRemove());
                    }
                }
                
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"[{GetType().Name}]");
            }
            
            await Task.Delay(30_000, stoppingToken);
        }
    }
}