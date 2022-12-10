namespace WB.DAL.Models;

public class SalesInfo
{
    public long SaleInfoId { get; set; }
    
    public long UserChatId { get; set; }

    public string Articul { get; set; }
    
    public double Income { get; set; }
    
    public DateTime SaleDate { get; set; }
}