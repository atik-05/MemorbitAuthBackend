using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Memorbit.Presentation.Authentication
{
    public static class PresentationAuthServiceExtensions
    {
        public static IServiceCollection AddPresentationAuth(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RefreshTokenCookieOptions>(
                configuration.GetSection(RefreshTokenCookieOptions.SectionName));
            services.AddSingleton<RefreshTokenCookieService>();

            return services;
        }
    }
}
