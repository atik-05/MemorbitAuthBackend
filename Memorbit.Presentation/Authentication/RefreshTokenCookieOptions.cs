namespace Memorbit.Presentation.Authentication
{
    public class RefreshTokenCookieOptions
    {
        public const string SectionName = "Auth:RefreshTokenCookie";

        public string Name { get; set; } = "memorbit_refresh_token";

        public string Path { get; set; } = "/api/auth";

        public string SameSite { get; set; } = "Lax";

        public bool Secure { get; set; } = true;
    }
}
