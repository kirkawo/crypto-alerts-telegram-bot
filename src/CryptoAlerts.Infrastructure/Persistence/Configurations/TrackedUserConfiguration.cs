using CryptoAlerts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoAlerts.Infrastructure.Persistence.Configurations;

public class TrackedUserConfiguration : IEntityTypeConfiguration<TrackedUser>
{
    public void Configure(EntityTypeBuilder<TrackedUser> builder)
    {
        builder.ToTable("TrackedUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.TelegramChatId)
            .IsRequired();

        builder.Property(u => u.TelegramUserId)
            .IsRequired();

        builder.Property(u => u.Username)
            .HasMaxLength(128);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(u => new { u.TelegramChatId, u.TelegramUserId })
            .IsUnique();
    }
}
