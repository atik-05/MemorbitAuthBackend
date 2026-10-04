using Memorbit.Application.Authentication.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Memorbit.Infrastructure.Authentication
{
    public static class AuthorizationPolicies
    {
        public static void ConfigureAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorizationBuilder()
                .AddPolicy(
                    AuthorizationPolicyNames.GuestUser,
                    policy => policy.RequireClaim("account_type", "guest"))
                .AddPolicy(
                    AuthorizationPolicyNames.RegisteredUser,
                    policy => policy.RequireClaim("account_type", "registered"));
        }
    }
}
