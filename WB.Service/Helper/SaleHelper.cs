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

        return $"*{sale.Date.ToString("dd.MM.yy HH:mm")}*" + Environment.NewLine +
               //$"**{saleType}** : {Math.Round(sale.TotalPrice*(1 - sale.DiscountPercent*1.0/100),1)}"+ Environment.NewLine +
               $"🛒*{saleType}* : {sale.ForPay}" + Environment.NewLine +
               $"🆔 Артикул WB: [{sale.NmId}](https://www.wildberries.ru/catalog/{sale.NmId}/detail.aspx)" +
               Environment.NewLine +
               $"🏷{sale.Brand} / [{sale.SupplierArticle}](https://www.wildberries.ru/catalog/{sale.NmId}/detail.aspx)" +
               Environment.NewLine +
               $"🌐{sale.WarehouseName} → {sale.OblastOkrugName}/{sale.RegionName}";
    }
}