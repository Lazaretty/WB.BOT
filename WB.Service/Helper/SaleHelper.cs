using WB.DAL.Models;
using WB.DAL.Repositories;
using WB.Service.Models;

namespace WB.Service.Helper;

public class SaleHelper
{
    private readonly SaleInfoRepository _saleInfoRepository;
    
    public SaleHelper(SaleInfoRepository saleInfoRepository)
    {
        _saleInfoRepository = saleInfoRepository;
    }

    public async Task<string> ToMessage(long chatId ,Sale sale)
    {
        var saleType = "Обновление";

        if (sale.SaleID.StartsWith("S", StringComparison.OrdinalIgnoreCase))
        {
            saleType = "Продажа";
        }

        if (sale.SaleID.StartsWith("R", StringComparison.OrdinalIgnoreCase))
        {
            saleType = "Возврат";
        }
        
        if (sale.SaleID.StartsWith("D", StringComparison.OrdinalIgnoreCase))
        {
            saleType = "Доплата";
        }
        
        var todayTotalSales = await _saleInfoRepository.GetAllSalesForToday(chatId);
        var todaySalesByArticul = await _saleInfoRepository.GetAllSalesByArticleForToday(sale.NmId.ToString(), chatId);
        var yesterdaySalesByArticul = await _saleInfoRepository.GetAllSalesByArticleForYesterday(sale.NmId.ToString(), chatId);

        var result = $"*{sale.Date.ToString("dd.MM.yy HH:mm")}*" + Environment.NewLine +
                     //$"**{saleType}** : {Math.Round(sale.TotalPrice*(1 - sale.DiscountPercent*1.0/100),1)}"+ Environment.NewLine +
                     $"🛒*{saleType}* : {Math.Round(sale.ForPay,1)}" + Environment.NewLine;
        
        if (todayTotalSales.Any())
        {
            result += $"📈 *Cегодня {todayTotalSales.Count}* на {Math.Round(todayTotalSales.Sum(x => x.Income),1)}" + Environment.NewLine;
        }

        result += $"🆔 Артикул WB: [{sale.NmId}](https://www.wildberries.ru/catalog/{sale.NmId}/detail.aspx)" +
                  Environment.NewLine +
                  $"🏷{sale.Brand} / [{sale.SupplierArticle}](https://www.wildberries.ru/catalog/{sale.NmId}/detail.aspx)" +
                  Environment.NewLine;
        
        if(todaySalesByArticul.Any())
        {
            result += $"💵 *Cегодня таких {todaySalesByArticul.Count}* на {Math.Round(todaySalesByArticul.Sum(x => x.Income),1)}" + Environment.NewLine;
        }
        
        if(yesterdaySalesByArticul.Any())
        {
            result += $"💶 *Вчера таких {yesterdaySalesByArticul.Count}* на {Math.Round(yesterdaySalesByArticul.Sum(x => x.Income),1)}" + Environment.NewLine;
        }
        
        result +=  $"🌐{sale.WarehouseName} → {sale.OblastOkrugName}/{sale.RegionName}";


        return result;
    }
}