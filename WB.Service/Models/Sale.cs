namespace WB.Service.Models;

public class Sale
{
    public DateTime Date { get; set; }
    public DateTime LastChangeDate { get; set; }
    public string SupplierArticle { get; set; }
    public string TechSize { get; set; }
    public int TotalPrice { get; set; }
    public int DiscountPercent { get; set; }
}