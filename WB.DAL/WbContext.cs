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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new UserConfiguration());
        }
    }
}

