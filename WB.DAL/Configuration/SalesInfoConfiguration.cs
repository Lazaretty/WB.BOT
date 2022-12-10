using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WB.DAL.Models;

namespace WB.DAL.Configuration;

public class SalesInfoConfiguration: IEntityTypeConfiguration<SalesInfo>
{
    public void Configure(EntityTypeBuilder<SalesInfo> builder)
    {
        builder.HasKey(x => x.SaleInfoId);
        
        builder.Property(x => x.SaleDate).IsRequired(true);
        builder.Property(x => x.Income).IsRequired(true);
        builder.Property(x => x.Articul).IsRequired(true);
    }
}