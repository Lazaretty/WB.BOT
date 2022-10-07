namespace WB.Service.Models;

public class CalculationInfo
{
    public string Articul { get; set; }

    public decimal Income { get; set; }

    public decimal Taxes => Income * TaxRate / 100;

    public decimal IncomeAfterTaxes => Income - Taxes;

    public int TaxRate { get; set; } = 6;
}