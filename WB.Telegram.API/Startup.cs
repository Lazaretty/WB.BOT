using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Examples.WebHook.Services;
using WB.DAL;
using WB.Service.Helper;
using WB.Service.Models;
using WB.Service.Services;

namespace WB.Telegram.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var botConfig = Configuration.GetSection("BotConfiguration").Get<TelegramConfiguration>();

            services.AddScoped<HandleUpdateService>();
            services.AddScoped<WBAdapter>();
            services.AddScoped<SaleHelper>();
            services.AddScoped<ProxyParser>();
            //services.AddHostedService<ConfigureWebhook>();
            services.AddHostedService<PoolingService>();
            
            services.AddHostedService<SalesNotifyService>();
            services.AddHostedService<ProxyWatchDogService>();

            /*
            services.AddHttpClient("tgwebhook")
                .AddTypedClient<ITelegramBotClient>(httpClient =>
                    new TelegramBotClient(botConfig.BotToken, httpClient));
            */
            services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

            services.AddLogging();

            services.AddControllers();
            
            services.AddHealthChecks();
            
            services.AddDALServices(Configuration);
            
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IHostApplicationLifetime applicationLifetime)
        {
            app.UseHealthChecks("/healthCheck", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    await context.Response.WriteAsync(
                        JsonConvert.SerializeObject(
                            new
                            {
                                result = "wb.bot is running"
                            }));
                }
            });
            
            app.UseRouting();
            app.UseCors();
            
            #region Migrate
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<WbContext>())
                {
                    context?.Database.Migrate();
                }
            }
            #endregion
            
            app.UseEndpoints(endpoints =>
            {
                var botConfig = Configuration.GetSection("BotConfiguration").Get<TelegramConfiguration>();
                // Configure custom endpoint per Telegram API recommendations:
                // https://core.telegram.org/bots/api#setwebhook
                // If you'd like to make sure that the Webhook request comes from Telegram, we recommend
                // using a secret path in the URL, e.g. https://www.example.com/<token>.
                // Since nobody else knows your bot's token, you can be pretty sure it's us.
                var token = botConfig.BotToken;
                endpoints.MapControllerRoute(name: "tgwebhook",
                    pattern: $"bot/{token}",
                    new { controller = "Webhook", action = "Post" });
                endpoints.MapControllers();
            });
        }
    }
}