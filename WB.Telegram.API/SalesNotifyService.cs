using System.Drawing;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
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
        
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var users = await repository.GetAllActiveAsync();

                foreach (var user in users)
                {
                    var sales = await wbAdapter.GetSales(user.ApiKey, user.LastUpdate.Value);

                    foreach (var sale in sales)
                    {

                        var vol = sale.NmId / 100_000;
                        var part = sale.NmId / 1_000;

                        var basket = vol >= 0 && vol <= 143 ? "//basket-01.wb.ru/" :
                            vol >= 144 && vol <= 287 ? "//basket-02.wb.ru/" :
                            vol >= 288 && vol <= 431 ? "//basket-03.wb.ru/" :
                            vol >= 432 && vol <= 719 ? "//basket-04.wb.ru/" :
                            vol >= 720 && vol <= 1007 ? "//basket-05.wb.ru/" :
                            vol >= 1008 && vol <= 1061 ? "//basket-06.wb.ru/" :
                            vol >= 1062 && vol <= 1115 ? "//basket-07.wb.ru/" :
                            vol >= 1116 && vol <= 1169 ? "//basket-08.wb.ru/" :
                            vol >= 1170 && vol <= 1313 ? "//basket-09.wb.ru/" :
                            vol >= 1314 && vol <= 1601 ? "//basket-10.wb.ru/" : "//basket-11.wb.ru/";


                        httpClient.BaseAddress = new Uri(basket);

                        var saleId = sale.NmId.ToString();

                        //var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c246x328/1.jpg";
                        //var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c516x688/1.jpg";
                        //var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/big/1.jpg";

                        var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c246x328/1.jpg";
                        var url1 = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c246x328/2.jpg";
                        var url2 = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c246x328/3.jpg";


                       var webRequest = System.Net.HttpWebRequest.Create(url);

                       var photoStream = new MemoryStream();
                       
                       using var webResponse = await webRequest.GetResponseAsync();
                       await using var stream = webResponse.GetResponseStream();
                       await stream.CopyToAsync(photoStream, stoppingToken);

                       photoStream.Position = 0;

                       var webRequest1 = System.Net.HttpWebRequest.Create(url1);
                       var photoStream1 = new MemoryStream();
                       
                       using var webResponse1 = await webRequest1.GetResponseAsync();
                       await using var stream1 = webResponse1.GetResponseStream();
                       await stream1.CopyToAsync(photoStream1, stoppingToken);

                       photoStream1.Position = 0;
                       
                       var webRequest2 = System.Net.HttpWebRequest.Create(url2);
                       var photoStream2 = new MemoryStream();
                       
                       using var webResponse2 = await webRequest2.GetResponseAsync();
                       await using var stream2 = webResponse2.GetResponseStream();
                       await stream2.CopyToAsync(photoStream2, stoppingToken);

                       photoStream2.Position = 0;

                       var bitMap = new Bitmap(photoStream);
                       var bitMap1 = new Bitmap(photoStream1);
                       var bitMap2 = new Bitmap(photoStream2);

                       var bitMaps = new List<Bitmap>() { bitMap, bitMap1, bitMap2};
                       
                       var width = 0;
                       var height = 0;

                       foreach (var image in bitMaps)
                       {
                           width += image.Width;
                           height = image.Height > height
                               ? image.Height
                               : height;
                       }

                       var border = 10;
                       
                       height += border * 2;
                       width += border * 4;
                       
                       var bitmap = new Bitmap(width, height);
                       
                       using (var g = Graphics.FromImage(bitmap))
                       {
                           g.Clear(Color.Snow);
                           
                           var localWidth = border;
                           foreach (var image in bitMaps)
                           {
                               g.DrawImage(image, localWidth, border);
                               localWidth += image.Width + border;
                           }
                       }

                       var resultStream = new MemoryStream();
                       
                       bitmap.Save(resultStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                       resultStream.Position = 0;
                       
                        if (resultStream.Length > 0)
                        {
                            await botClient.SendPhotoAsync(
                                chatId: user.UserChatId,
                                new InputOnlineFile(resultStream),
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
            catch (Exception e)
            {
                _logger.LogCritical(e, $"[{GetType().Name}]");
            }

            await Task.Delay(30_000, stoppingToken);
        }
    }
}