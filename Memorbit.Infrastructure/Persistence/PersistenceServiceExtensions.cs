using Memorbit.Infrastructure.Authentication;
using Memorbit.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Memorbit.Infrastructure.Persistence
{
    public static class PersistenceServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing 'DefaultConnection' connection string.");

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            services.ConfigureIdentity();
            services.AddAuthenticationServices(configuration);

            return services;
        }
    }
}
