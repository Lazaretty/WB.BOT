using WB.Service.Models;

namespace WB.Service.Helper;

public static class SaleHelper
{
    public static string ToMessage(this Sale sale)
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
        
        return $"{saleType}: " + Environment.NewLine + 
               $"Артикул : {sale.SupplierArticle}" + Environment.NewLine +
               $"Цена : {Math.Round(sale.TotalPrice*(1 - sale.DiscountPercent*1.0/100),1)}"+ Environment.NewLine +
               $"Дата : {sale.Date.ToString("hh:mm dd.mm.yyy")}"+ Environment.NewLine +
               $"Ссылка : https://www.wildberries.ru/catalog/{sale.NmId}/detail.aspx";
    }
}