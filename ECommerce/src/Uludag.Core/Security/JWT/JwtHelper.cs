using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Uludag.Core.Security.Constants;

namespace Uludag.Core.Security.JWT
{
    // JWT = Header.Payload.Signature (her parça Base64Url)
    // Payload ŞİFRELİ DEĞİLDİR, herkes okuyabilir (jwt.io) => içine şifre vb. hassas bilgi konmaz.
    // Güvenliği imza sağlar: içerik değiştirilirse imza tutmaz.
    //
    // Payload'da kullanıcıya ait TEK bilgi oturum anahtarıdır ("sid"). Email, yetkiler vb. Redis'teki oturumdan okunur.
    // Faydaları: token okunduğunda kişisel veri sızmaz, token küçük kalır ve oturum Redis'ten silinerek
    // token süresi dolmadan iptal edilebilir (logout, yetki değişikliği).
    public class JwtHelper : ITokenHelper
    {
        private readonly TokenOptions _tokenOptions;
        private readonly SigningCredentials _signingCredentials;
        private readonly JsonWebTokenHandler _tokenHandler = new();

        public JwtHelper(TokenOptions tokenOptions)
        {
            _tokenOptions = tokenOptions;
            _signingCredentials = SecurityKeyHelper.CreateSigningCredentials(
                SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey));
        }

        public AccessToken CreateToken(string sessionKey, DateTime expiration)
        {
            var now = DateTime.UtcNow;

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _tokenOptions.Issuer,
                Audience = _tokenOptions.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = expiration,
                SigningCredentials = _signingCredentials,
                // Tek claim: Redis'teki oturumun anahtarı. İstek geldiğinde bu anahtarla oturum Redis'ten okunur
                // ve kullanıcı claim'leri (sub, email, role) HttpContext.User'a eklenir (bkz. WebAPI => SessionTokenValidator).
                Subject = new ClaimsIdentity([new Claim(SecurityClaimTypes.SessionKey, sessionKey)])
            };

            return new AccessToken
            {
                Token = _tokenHandler.CreateToken(descriptor),
                Expiration = expiration
            };
        }
    }
}
