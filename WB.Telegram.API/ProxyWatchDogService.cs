using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using Telegram.Bot;
using WB.DAL.Models;
using WB.DAL.Repositories;
using WB.Service.Models;

namespace WB.Telegram.API;

public class ProxyWatchDogService: BackgroundService
{
    private readonly ILogger<SalesNotifyService> _logger;
    private readonly IServiceProvider _services;
    private readonly TelegramConfiguration _botConfig;
    private readonly ConcurrentBag<long> _proxyToChange = new();

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

                var tasks = new List<Task>();
                
                foreach (var proxy in proxies)
                {
                    if (tasks.Count == 20)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    tasks.Add(Ping(proxy));
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                }

                foreach (var proxyId in _proxyToChange)
                {
                    var proxy = proxies.FirstOrDefault(x => x.ProxyId == proxyId);
                    if (proxy != null) await proxyRepository.Update(proxy);
                }

                _proxyToChange.Clear();
                _logger.LogInformation($"{_proxyToChange} проксей поменяли статус");
                _logger.LogInformation($"{proxies.Count(x => x.Active)} активных");
                await botClient.SendTextMessageAsync(chatId: 669363145, $"{proxies.Count(x => x.Active)} active proxies");

                await Task.Delay(TimeSpan.FromMinutes(2));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"[{GetType().Name}]");
            }
        }
    }

    private async Task Ping(Proxy proxy)
    {
        await Task.Delay(1);
        
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
            if (proxy.Active != result)
            {
                proxy.Active = result; 
                _proxyToChange.Add(proxy.ProxyId);
            }
        }
    }
}