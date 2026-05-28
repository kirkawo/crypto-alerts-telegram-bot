using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoAlerts.Infrastructure.Persistence.Configurations;

public class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("PriceAlerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.AssetSymbol)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(a => a.AssetId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.TargetPrice)
            .IsRequired()
            .HasColumnType("decimal(18,8)");

        builder.Property(a => a.Condition)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.TriggeredAtUtc);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Status);
    }
}
