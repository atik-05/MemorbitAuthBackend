namespace Memorbit.Application.Authentication.Models
{
    public record IssuedAuthResult(
        AuthResponse Response,
        string RefreshTokenPlain);
}
