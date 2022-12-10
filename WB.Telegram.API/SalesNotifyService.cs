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
        
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var repository = scope.ServiceProvider.GetRequiredService<UserRepository>();
                var wbAdapter =  scope.ServiceProvider.GetRequiredService<WBAdapter>();
                var saleInfoRepository = scope.ServiceProvider.GetRequiredService<SaleInfoRepository>();
                var saleHelper = scope.ServiceProvider.GetRequiredService<SaleHelper>();
                var builder = new MessageBuilder();
                
                await wbAdapter.Init();
                
                var users = await repository.GetAllActiveAsync();

                var notifyTasks = new List<Task>();

                var count = 0;
                
                foreach (var user in users.Where(x => !string.IsNullOrEmpty(x.ApiKey)))
                {
                    _logger.LogInformation($"Handle updates for {user.UserChatId} last update {user.LastUpdate.Value.ToString("dd.MM.yy HH:mm")}");
                    
                    var sales = await wbAdapter.GetSales(user.ApiKey, user.LastUpdate.Value);

                    if (sales == null || !sales.Any())
                    {
                        _logger.LogInformation($"No sales found for {user.UserChatId}");
                        continue;
                    }

                    if (count == 50)
                    {
                        await Task.WhenAll(notifyTasks);
                        notifyTasks.Clear();
                    }

                    notifyTasks.Add(Notify(user, sales, repository, saleInfoRepository, saleHelper, builder, botClient));
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

    private async Task Notify(User user ,IEnumerable<Sale> sales, UserRepository repository, SaleInfoRepository saleInfoRepository , SaleHelper saleHelper  ,MessageBuilder builder, ITelegramBotClient botClient)
    {
        _logger.LogInformation($"Send message for {user.UserChatId} about {sales.Count()} sales");
        
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

            await saleInfoRepository.InsertAsync(new SalesInfo()
            {
                UserChatId = user.UserChatId,
                Articul = sale.NmId.ToString(),
                Income = sale.ForPay,
                SaleDate = sale.Date
            });
            
            if (content.Length > 0)
            {
                await botClient.SendPhotoAsync(
                    chatId: user.UserChatId,
                    new InputOnlineFile(content),
                    caption: await saleHelper.ToMessage(user.UserChatId, sale),
                    parseMode: ParseMode.Markdown);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: user.UserChatId,
                    text: await saleHelper.ToMessage(user.UserChatId, sale),
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