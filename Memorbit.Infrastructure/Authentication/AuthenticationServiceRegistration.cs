using Memorbit.Application.Authentication.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Memorbit.Infrastructure.Authentication
{
    public static class AuthenticationServiceRegistration
    {
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureJwtAuthentication(configuration);
            services.ConfigureAuthorizationPolicies();

            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ICurrentUser, CurrentUser>();

            services.AddHostedService<RefreshTokenCleanupService>();

            return services;
        }
    }
}
