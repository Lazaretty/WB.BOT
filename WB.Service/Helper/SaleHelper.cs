﻿using WB.Service.Models;

namespace WB.Service.Helper;

public static class SaleHelper
{
    public static string ToMessage(this Sale sale)
    {
        return $"Продажа: " + Environment.NewLine + "Артикул : {sale.SupplierArticle}" + Environment.NewLine +
               $"Цена : {sale.TotalPrice}";
    }
}