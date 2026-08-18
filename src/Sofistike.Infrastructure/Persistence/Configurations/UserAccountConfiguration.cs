using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Users;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration
    : IEntityTypeConfiguration<UserAccount>
{
    private static readonly DateTimeOffset DevelopmentUserCreatedAt =
        new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

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

        builder.HasData(
            new UserAccount
            {
                Id = Guid.Parse("d8fbd714-b22f-4a7f-b576-c6a2183f6e80"),
                Email = "umay@sofistike.com",
                NormalizedEmail = "UMAY@SOFISTIKE.COM",
                FirstName = "Umay",
                LastName = null,
                PhoneNumber = null,
                Role = "Customer",
                PasswordSalt = "A28ElxOKGPX0xqNXBZE9Ug==",
                PasswordHash = "StGqoqkeBqdPX6jwzsH95lJoqA8/Ej8s3ruOXAaE754=",
                PasswordIterations = 120000,
                IsActive = true,
                CreatedAtUtc = DevelopmentUserCreatedAt,
            }
        );
    }
}
