using System.Security.Claims;
using Memorbit.Application.Authentication.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Memorbit.Infrastructure.Authentication
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public Guid UserId
        {
            get
            {
                var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var userId))
                {
                    throw new InvalidOperationException("No authenticated user is available in the current context.");
                }

                return userId;
            }
        }

        public bool IsGuest => Principal?.FindFirstValue("account_type") == "guest";
    }
}
