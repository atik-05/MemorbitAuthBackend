namespace Memorbit.Application.Authentication.Models
{
    public record AuthResponse(
        Guid UserId,
        bool IsGuest,
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc);
}
