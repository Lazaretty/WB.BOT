// See https://aka.ms/new-console-template for more information

namespace WB.BOT;

public class Program
{
    static void Main()
    {
        string FileName = @"C:\Users\dskvorts2\docs\0.xlsx";
        var parser = new DataParser(FileName, 0);
        parser.ReadData();

        Console.WriteLine(parser.GenerateReportIncomeByArticul());
    }
}