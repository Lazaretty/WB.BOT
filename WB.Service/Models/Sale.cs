namespace WB.Service.Models;

public class Sale
{
    public DateTime Date { get; set; }
    public DateTime LastChangeDate { get; set; }
    public string SupplierArticle { get; set; }
    public string TechSize { get; set; }
    public int TotalPrice { get; set; }
    public int DiscountPercent { get; set; }
    public int NmId { get; set; }
    public string SaleID { get; set; }
    public string Brand { get; set; }
    public double ForPay { get; set; }
    
    public string WarehouseName { get; set; }
    public string OblastOkrugName { get; set; }
    public string RegionName { get; set; }
}