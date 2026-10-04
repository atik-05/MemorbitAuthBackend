using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Memorbit.Presentation.Authentication
{
    public class RefreshTokenCookieService
    {
        private readonly RefreshTokenCookieOptions _options;

        public RefreshTokenCookieService(IOptions<RefreshTokenCookieOptions> options)
        {
            _options = options.Value;
        }

        public void Set(HttpResponse response, string plainToken, DateTime expiresAtUtc)
        {
            response.Cookies.Append(_options.Name, plainToken, BuildCookieOptions(expiresAtUtc));
        }

        public bool TryGet(HttpRequest request, out string refreshToken)
        {
            return request.Cookies.TryGetValue(_options.Name, out refreshToken!);
        }

        public void Clear(HttpResponse response)
        {
            response.Cookies.Append(_options.Name, string.Empty, new CookieOptions
            {
                HttpOnly = true,
                Secure = _options.Secure,
                SameSite = ParseSameSite(_options.SameSite),
                Path = _options.Path,
                Expires = DateTimeOffset.UnixEpoch
            });
        }

        private CookieOptions BuildCookieOptions(DateTime expiresAtUtc) => new()
        {
            HttpOnly = true,
            Secure = _options.Secure,
            SameSite = ParseSameSite(_options.SameSite),
            Path = _options.Path,
            Expires = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)
        };

        private static SameSiteMode ParseSameSite(string value) => value.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "strict" => SameSiteMode.Strict,
            _ => SameSiteMode.Lax
        };
    }
}
