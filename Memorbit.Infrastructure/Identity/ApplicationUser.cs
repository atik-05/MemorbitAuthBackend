using Microsoft.AspNetCore.Identity;

namespace Memorbit.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public bool IsGuest { get; set; }

        public string? DisplayName { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }

        public DateTime? UpgradedAtUtc { get; set; }
    }
}
