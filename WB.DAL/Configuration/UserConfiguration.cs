using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WB.DAL.Models;

namespace WB.DAL.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.UserId);
        
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.ApiKey).IsRequired(false);

        builder.HasOne(x => x.ChatState)
            .WithOne(x => x.User)
            .HasForeignKey<User>(x => x.UserId);
    }
}