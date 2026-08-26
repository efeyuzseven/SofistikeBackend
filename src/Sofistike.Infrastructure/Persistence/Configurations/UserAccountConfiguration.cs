using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Users;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration
    : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users", "auth");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();

        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100);
        builder.Property(user => user.PhoneNumber).HasMaxLength(30);
        builder.Property(user => user.Role).HasMaxLength(50).IsRequired();
        builder.Property(user => user.PasswordSalt)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(user => user.PasswordHash)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(user => user.CreatedAtUtc).HasPrecision(0);

    }
}
