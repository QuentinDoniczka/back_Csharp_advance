namespace BackBase.Infrastructure.Data.Configurations;

using BackBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("RevokedTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Jti)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.RevokedAt)
            .IsRequired();

        builder.HasIndex(t => t.Jti)
            .IsUnique();

        builder.HasIndex(t => t.ExpiresAt);
    }
}
