using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Uludag.Core.Security;

namespace ECommerce.WebAPI.Security
{
    // ICurrentUser'ın HTTP'ye özgü implementasyonu.
    // JwtBearer middleware token'ı doğruladıktan sonra claim'leri HttpContext.User'a koyar; burada onları okuruz.
    public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public Guid? UserId =>
            Guid.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

        public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email);
    }
}
