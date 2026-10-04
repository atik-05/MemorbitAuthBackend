namespace Memorbit.Domain.Authentication
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public Guid FamilyId { get; set; } = Guid.NewGuid();

        public string TokenHash { get; set; } = null!;

        public string SecurityStamp { get; set; } = null!;

        public string? DeviceName { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public DateTime? LastUsedAtUtc { get; set; }

        public Guid? ReplacedByTokenId { get; set; }

        public string? CreatedByIp { get; set; }

        public string? UserAgent { get; set; }

        public bool IsRevoked => RevokedAtUtc.HasValue;

        public bool IsExpiredAt(DateTime utcNow) => utcNow >= ExpiresAtUtc;

        public bool IsActiveAt(DateTime utcNow) => !IsRevoked && !IsExpiredAt(utcNow);
    }
}
