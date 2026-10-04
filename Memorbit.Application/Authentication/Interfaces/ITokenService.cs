using Memorbit.Application.Authentication.Models;

namespace Memorbit.Application.Authentication.Interfaces
{
    public interface ITokenService
    {
        AccessToken CreateAccessToken(
            Guid userId,
            bool isGuest,
            string securityStamp,
            IReadOnlyCollection<string> roles);

        string CreateRefreshToken();

        string HashToken(string token);
    }
}
