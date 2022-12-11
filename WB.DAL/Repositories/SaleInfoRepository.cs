using Microsoft.EntityFrameworkCore;
using WB.DAL.Models;

namespace WB.DAL.Repositories;

public class SaleInfoRepository
{
    public SaleInfoRepository(WbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public WbContext Context { get; }
    
    public async Task InsertAsync(SalesInfo salesInfo)
    {
        Context.SalesInfos.Add(salesInfo);

        await Context.SaveChangesAsync();
    }
    
    public async Task<List<SalesInfo>> GetAllSalesForToday(long userChatId)
    {
        var today = DateTime.Today;
        
        var sales = await Context.SalesInfos
            .Where(x => x.SaleDate > today && x.UserChatId == userChatId)
            .ToListAsync();
        return sales;
    }
    
    public async Task<List<SalesInfo>> GetAllSalesByArticleForToday(string articul, long userChatId)
    {
        var today = DateTime.Today;
        
        var sales = await Context.SalesInfos
            .Where(x => x.SaleDate > today && x.Articul == articul && x.UserChatId == userChatId)
            .ToListAsync();
        return sales;
    }
    
    public async Task<List<SalesInfo>> GetAllSalesByArticleForYesterday(string articul, long userChatId)
    {
        var today = DateTime.Today;
        var yesterday = DateTime.Today.AddDays(-1);
        
        var sales = await Context.SalesInfos
            .Where(x => x.SaleDate < today && x.SaleDate > yesterday && x.Articul == articul && x.UserChatId == userChatId)
            .ToListAsync();
        return sales;
    }
}