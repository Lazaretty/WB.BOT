
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

            var saleInfo = new SaleInfo()
            {
                IsSell = isSell,
                Income = income,
                Barcode = barcode
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

        foreach (var value in TotalIncome)
        {
            result += $"{value.Key} : {value.Value.Sum(x => x.Income)}" + Environment.NewLine;
        }

        result += $"Total income: {TotalIncome.SelectMany(x => x.Value).Sum(x => x.Income)}";
        
        return result;
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
}