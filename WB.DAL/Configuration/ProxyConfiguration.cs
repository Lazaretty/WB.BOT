using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WB.DAL.Models;

namespace WB.DAL.Configuration;

public class ProxyConfiguration : IEntityTypeConfiguration<Proxy>
{
     
     public void Configure(EntityTypeBuilder<Proxy> builder)
     {
          builder.HasKey(x => x.ProxyId);

     }
}