
using Aspose.Cells;


namespace WB.BOT;

public class DataParser
{
    public Worksheet Worksheet { get; set; }

    private int Rows { get; }

    private int Columns { get; }

    private Dictionary<string, int> ColumnsNames { get; }

    public Dictionary<string, List<SaleInfo>> TotalIncome { get; set; }

    public DataParser(string filePath, int workSheet)
    {
        var workbook = new Workbook(filePath);

        Worksheet = workbook.Worksheets[0];

        Rows = Worksheet.Cells.Rows.Count;

        Columns = Worksheet.Cells.Columns.Count;

        ColumnsNames = new Dictionary<string, int>
        {
            { Constants.ColumnNames.Articul, GetColumnByName(Constants.ColumnNames.Articul) },
            { Constants.ColumnNames.Fees, GetColumnByName(Constants.ColumnNames.Fees) },
            { Constants.ColumnNames.DeleveryFee, GetColumnByName(Constants.ColumnNames.DeleveryFee) },
            { Constants.ColumnNames.DocumentType, GetColumnByName(Constants.ColumnNames.DocumentType) },
            { Constants.ColumnNames.OwnerIncome, GetColumnByName(Constants.ColumnNames.OwnerIncome) },
            { Constants.ColumnNames.WbIncome, GetColumnByName(Constants.ColumnNames.WbIncome) },
            { Constants.ColumnNames.Barcode, GetColumnByName(Constants.ColumnNames.Barcode) }
        };

        TotalIncome = new Dictionary<string, List<SaleInfo>>();

        if (ColumnsNames.Values.All(x => x != -1)) return;
        {
            var notFoundColumns = ColumnsNames.Where(x => x.Value == -1).Select(x => x.Key).ToArray();
            throw new Exception($"Some columns are not found: {string.Join(',', notFoundColumns)}");
        }
    }

    public void ReadData()
    {
        for (var i = 1; i < Rows; i++)
        {
            var articul = Worksheet.Cells[i, ColumnsNames[Constants.ColumnNames.Articul]].Value.ToString();

            var isSell = Worksheet.Cells[i, ColumnsNames[Constants.ColumnNames.DocumentType]].Value.ToString() ==
                         "Продажа";

            decimal.TryParse(Worksheet.Cells[i, ColumnsNames[Constants.ColumnNames.OwnerIncome]].Value.ToString(),
                out var income);

            var result = isSell ? income : -1 * income;

            var barcode = Worksheet.Cells[i, ColumnsNames[Constants.ColumnNames.Barcode]].Value.ToString();
            
            decimal.TryParse(Worksheet.Cells[i, ColumnsNames[Constants.ColumnNames.DeleveryFee]].Value.ToString(),
                out var deliveryFee);

            var saleInfo = new SaleInfo()
            {
                IsSell = isSell,
                Income = result,
                Barcode = barcode,
                DeliveryFee = deliveryFee
            };

            if (TotalIncome.ContainsKey(articul))
            {
                var saleINfo = TotalIncome[articul];

                saleINfo.Add(saleInfo);
            }
            else
            {
                TotalIncome.Add(articul, new List<SaleInfo>()
                {
                    saleInfo
                });
            }
        }
    }

    public string GenerateReportIncomeByArticul()
    {
        var result = "Articul : Income" + Environment.NewLine;

        decimal sum = 0;
        decimal delivery = 0;
        
        foreach (var value in TotalIncome)
        {
            var sumByArticul = value.Value.Sum(x => x.Income);
            var deliveryByArticul = value.Value.Sum(x => x.DeliveryFee);

            sum += sumByArticul;
            delivery += deliveryByArticul;
            
            result += $"{value.Key} : {sumByArticul - deliveryByArticul}" + Environment.NewLine;
        }
        
        result += $"Total income: {sum - delivery}";
        
        return result;
    }

    public void GenerateReportIncomeByArticulToFile()
    {
        // initiate an instance of Workbook
        var book = new Aspose.Cells.Workbook();
        // access first (default) worksheet
        var sheet = book.Worksheets[0];
        // access CellsCollection of first worksheet
        var cells = sheet.Cells;

        cells[0,0].Value= "Articul";
        cells[0,1].Value= "Income";
        cells[0,2].Value= "Taxes";
        cells[0,3].Value= "Income after taxes";
        
        decimal sum = 0;
        decimal delivery = 0;

        var i = 1;

        foreach (var value in TotalIncome)
        {
            if (string.IsNullOrEmpty(value.Key))
            {
                foreach (var saleInfo in value.Value.GroupBy(x => x.Barcode))
                {
                    var sumByBarcode = value.Value.Sum(x => x.Income);
                    var deliveryByBarcode = value.Value.Sum(x => x.DeliveryFee);

                    sum += sumByBarcode;
                    delivery += deliveryByBarcode;
                    
                    cells[i, 0].Value = saleInfo.Key;
                    cells[i, 1].Value = sumByBarcode - deliveryByBarcode;
                    cells[i, 2].Formula = $"={IntegerToExcelColumn(1)}{i+1}*H3/100";
                    cells[i, 3].Formula = $"={IntegerToExcelColumn(1)}{i+1}-{IntegerToExcelColumn(2)}{i+1}";
                }
            }
            else
            {
                var sumByArticul = value.Value.Sum(x => x.Income);
                var deliveryByArticul = value.Value.Sum(x => x.DeliveryFee);
            
                sum += sumByArticul;
                delivery += deliveryByArticul;
                
                cells[i, 0].Value = value.Key;
                cells[i, 1].Value = sumByArticul - deliveryByArticul;
                cells[i, 2].Formula = $"={IntegerToExcelColumn(1)}{i+1}*H3/100";
                cells[i, 3].Formula = $"={IntegerToExcelColumn(1)}{i+1}-{IntegerToExcelColumn(2)}{i+1}";
            }

            i++;
        }
        
        cells[i,0].Value = "Total income";
        cells[i,1].Value = sum - delivery;
        
        cells["G3"].Value = "Tax rate";
        cells["H3"].Value = 6;

        // save spreadsheet to disc
        book.Save("output.xlsx", SaveFormat.Xlsx);
    }
    
    private int GetColumnByName(string columnName)
    {
        for (var i = 0; i < Columns; i++)
        {
            if (Worksheet.Cells[0, i].Value.ToString() == columnName)
            {
                return i;
            }
        }

        return -1;
    }

    private string IntegerToExcelColumn(int integer)
    {
        var multiplicator = integer / 26;

        return ((char)('A' + integer)).ToString();
    }
}