namespace Memorbit.Infrastructure.Identity
{
    public static class GuestDefaults
    {
        public const string EmailDomain = "guest.memorbit.local";

        public const string UsernamePrefix = "guest-";

        public static string GenerateGuestEmail(Guid userId) => $"{userId:N}@{EmailDomain}";

        public static string GenerateGuestUsername(Guid userId) => $"{UsernamePrefix}{userId:N}";

        public static bool IsReservedGuestEmail(string? email) =>
            !string.IsNullOrWhiteSpace(email) &&
            email.EndsWith("@" + EmailDomain, StringComparison.OrdinalIgnoreCase);
    }
}
