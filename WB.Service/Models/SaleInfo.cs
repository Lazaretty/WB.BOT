namespace WB.Service.Models;

public class SaleInfo
{
    public decimal Income { get; set; }
    
    public bool IsSell { get; set; }

    public string Barcode { get; set; }
    
    public decimal DeliveryFee { get; set; }
}