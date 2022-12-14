using Microsoft.EntityFrameworkCore;
using WB.DAL.Configuration;
using WB.DAL.Models;

namespace WB.DAL
{
    public class WbContext : DbContext
    {
        public WbContext(DbContextOptions options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        
        public DbSet<ChatState> ChatStates { get; set; }
        public DbSet<SalesInfo> SalesInfos { get; set; }
        public DbSet<Proxy> Proxies { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new UserConfiguration());
            //builder.ApplyConfiguration(new ChatStatesConfiguration());
            builder.ApplyConfiguration(new SalesInfoConfiguration());
            builder.ApplyConfiguration(new ProxyConfiguration());
        }
    }
}

