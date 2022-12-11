using System.Net.NetworkInformation;
using Telegram.Bot;
using WB.DAL.Repositories;
using WB.Service.Models;

namespace WB.Telegram.API;

public class ProxyWatchDogService: BackgroundService
{
    private readonly ILogger<SalesNotifyService> _logger;
    private readonly IServiceProvider _services;
    private readonly TelegramConfiguration _botConfig;

    public ProxyWatchDogService(
        ILogger<SalesNotifyService> logger,
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
                var proxyRepository = scope.ServiceProvider.GetRequiredService<ProxyRepository>();

                var proxies = await proxyRepository.GetAllAsync();

                if (proxies.Count < 50)
                {
                    await botClient.SendTextMessageAsync(chatId: 669363145, $"Активных прокси : {proxies.Count}");
                }
                
                if (proxies.Count == 0)
                {
                    await botClient.SendTextMessageAsync(chatId: 669363145, $"!!!НЕТ АКТИВНЫХ ПРОКСИ!!!");
                }

                foreach (var proxy in proxies)
                {
                    var result = false;
                    try
                    {
                        var pinger = new Ping();
                        var reply = pinger.Send(proxy.Host);
                        result = reply.Status == IPStatus.Success;
                    }
                    catch (PingException)
                    {
                        result = false;
                    }
                    finally
                    {
                        proxy.Active = result;
                        await proxyRepository.Update(proxy);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"[{GetType().Name}]");
            }
        }
    }
}