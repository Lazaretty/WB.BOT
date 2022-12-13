using Microsoft.EntityFrameworkCore;
using WB.DAL.Models;

namespace WB.DAL.Repositories;

public class ProxyRepository
{
    public ProxyRepository(WbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public WbContext Context { get; }
    
    public async Task InsertAsync(Proxy proxy)
    {
        Context.Proxies.Add(proxy);

        await Context.SaveChangesAsync();
    }
    
    public async Task Update(Proxy proxy)
    {
        if (proxy == null)
            throw new ArgumentNullException(nameof(proxy));

        Context.Entry(proxy).State = EntityState.Modified;
        await Context.SaveChangesAsync();
        Context.Entry(proxy).State = EntityState.Detached;
    }

    public async Task<List<Proxy>> GetFreshestProxies(int limit = 50)
    {
        var dtn = DateTime.Now;
        
        var proxies = await Context.Proxies
            .Where(x => x.Active && DateTime.Now - x.LastUsed > TimeSpan.FromMinutes(3))
            .OrderByDescending(x => x.SuccessfulUses)
            .ThenBy(x => x.LastUsed)
            .Take(limit)
            .ToListAsync();
        
        return proxies;
    }
    
    public async Task<List<Proxy>> GetAllAsync()
    {
        var proxies = await Context.Proxies.ToListAsync();
        
        return proxies;
    }
}