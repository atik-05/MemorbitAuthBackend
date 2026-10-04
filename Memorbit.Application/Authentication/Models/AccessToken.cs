namespace Memorbit.Application.Authentication.Models
{
    public record AccessToken(string Token, DateTime ExpiresAtUtc);
}
