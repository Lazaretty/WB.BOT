using Telegram.Bot;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Types;
using WB.DAL.Repositories;
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
        
        var botClient = _services.GetRequiredService<ITelegramBotClient>();

        await botClient.DeleteWebhookAsync(cancellationToken: stoppingToken);
        
        var offset = -1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Update[] updates;
                
                if(offset == -1)
                {
                    updates = await botClient.GetUpdatesAsync(cancellationToken: stoppingToken);
                }
                else
                {
                    updates = await botClient.GetUpdatesAsync(cancellationToken: stoppingToken, offset: offset + 1);
                }
                
                foreach (var update in updates)
                {
                    using var scope = _services.CreateScope();
                    var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
                    var chatStateRepository = scope.ServiceProvider.GetRequiredService<ChatStateRepository>();
                    var proxyRepository = scope.ServiceProvider.GetRequiredService<ProxyRepository>();
                    
                    var handleService = new HandleUpdateService(botClient,userRepository,chatStateRepository,proxyRepository);
                    
                    await handleService.EchoAsync(update);
                    offset = update.Id;
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