using System.ComponentModel.DataAnnotations;

namespace Memorbit.Infrastructure.Authentication
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Issuer { get; set; } = null!;

        [Required]
        public string Audience { get; set; } = null!;

        [Required]
        [MinLength(32, ErrorMessage = "Jwt:SecretKey must be at least 32 characters long.")]
        public string SecretKey { get; set; } = null!;

        [Range(1, 1440)]
        public int AccessTokenMinutes { get; set; } = 15;

        [Range(1, 365)]
        public int RefreshTokenDays { get; set; } = 30;

        [Range(1, 730)]
        public int GuestRefreshTokenDays { get; set; } = 180;
    }
}
