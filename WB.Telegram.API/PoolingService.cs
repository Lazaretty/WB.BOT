using Telegram.Bot;
using Telegram.Bot.Examples.WebHook.Services;
using WB.Service.Models;

namespace WB.Telegram.API;

public class PoolingService : BackgroundService
{
    private readonly ILogger<PoolingService> _logger;
    private readonly IServiceProvider _services;
    private readonly TelegramConfiguration _botConfig;
    
    public PoolingService(ILogger<PoolingService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _services = serviceProvider;
        _botConfig = configuration.GetSection("BotConfiguration").Get<TelegramConfiguration>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var handleService = new HandleUpdateService(botClient);
        
        await botClient.DeleteWebhookAsync(cancellationToken: stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updateds = await botClient.GetUpdatesAsync(cancellationToken: stoppingToken);

                foreach (var update in updateds)
                {
                    await handleService.EchoAsync(update);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"[{GetType().Name}]");
            }
            
            await Task.Delay(1_000, stoppingToken);
        }
    }
}