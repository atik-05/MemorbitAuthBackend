using Memorbit.Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Memorbit.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.Property(x => x.SecurityStamp)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.DeviceName)
                .HasMaxLength(200);

            builder.Property(x => x.CreatedByIp)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });

            builder.HasIndex(x => x.FamilyId);
        }
    }
}
