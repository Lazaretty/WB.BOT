using System.Drawing;
using System.Drawing.Imaging;
using Telegram.Bot.Types.InputFiles;
using WB.Service.Models;

namespace WB.Service.Services;

public class MessageBuilder
{
    public async Task BuildInputOnlineFileFromSaleInfo(Sale sale, Stream result)
    {
        var bitMaps = await Task.WhenAll(new[]
        {
            GetBitMapFromSale(sale, 1),
            GetBitMapFromSale(sale, 2),
            GetBitMapFromSale(sale, 3)
        });
        
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

            //var i = 0;
            
            foreach (var image in bitMaps)
            {
                //image.Save("123.jpg", ImageFormat.Jpeg);
                g.DrawImage(image, localWidth, border);
                localWidth += image.Width + border;
                
                //bitmap.Save($"{i}.jpg", ImageFormat.Jpeg);
                //i++;
            }
        }
        
        bitmap.Save(result, System.Drawing.Imaging.ImageFormat.Jpeg);
    }

    private async Task<Bitmap> GetBitMapFromSale(Sale sale, int photoNum)
    {
        sale.NmId = 64364103;
        //sale.NmId = 82110337;
        await Task.Delay(1);
        
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
        
        var saleId = sale.NmId.ToString();

        //var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c246x328/1.jpg";
        //var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c516x688/1.jpg";
        //var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/big/1.jpg";

        var url = "https:"+basket + $"vol{vol}/part{part}/{saleId}/images/c246x328/{photoNum}.jpg";
        
        var webRequest = System.Net.HttpWebRequest.Create(url);

        var photoStream = new MemoryStream();
                       
        using var webResponse = await webRequest.GetResponseAsync();
        await using var stream = webResponse.GetResponseStream();
        await stream.CopyToAsync(photoStream);

        photoStream.Position = 0;

        return new Bitmap(photoStream);
    }
}