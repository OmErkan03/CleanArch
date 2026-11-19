using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Uludag.Core.Security.Constants;
using Uludag.Core.Security.Sessions;

namespace ECommerce.WebAPI.Security
{
    // JwtBearer token'ın imzasını/süresini doğruladıktan SONRA çalışır (JwtBearerEvents.OnTokenValidated).
    // Token yalnızca oturum anahtarını ("sid") taşır; kullanıcı bilgileri burada Redis'ten okunup HttpContext.User'a eklenir.
    // Böylece CurrentUser, AuthenticationBehavior ve AuthorizationBehavior hiçbir şey değiştirmeden claim'leri okumaya devam eder.
    public static class SessionTokenValidator
    {
        public static async Task OnTokenValidated(TokenValidatedContext context)
        {
            var sessionKey = context.Principal?.FindFirstValue(SecurityClaimTypes.SessionKey);
            if (string.IsNullOrEmpty(sessionKey))
            {
                context.Fail("Token oturum anahtarı içermiyor.");
                return;
            }

            var sessionStore = context.HttpContext.RequestServices.GetRequiredService<ISessionStore>();
            var session = await sessionStore.GetAsync(sessionKey, context.HttpContext.RequestAborted);

            // İmza geçerli olsa bile oturum Redis'te yoksa (süresi doldu / silindi) token kabul edilmez => kullanıcı anonim kalır.
            if (session is null)
            {
                context.Fail("Oturum bulunamadı veya sonlandırılmış.");
                return;
            }

            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, session.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, session.Email)
            ];
            claims.AddRange(session.OperationClaims.Select(name => new Claim(SecurityClaimTypes.OperationClaim, name)));

            // Token'dan gelen kimliğe Redis'teki bilgiler eklenir (RoleClaimType ayarı korunsun diye aynı identity kullanılır).
            if (context.Principal!.Identity is ClaimsIdentity identity)
                identity.AddClaims(claims);
        }
    }
}
