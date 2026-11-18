using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Uludag.Core.Security.JWT
{
    // JWT = Header.Payload.Signature (her parça Base64Url)
    // Payload ŞİFRELİ DEĞİLDİR, herkes okuyabilir (jwt.io) => içine şifre vb. hassas bilgi konmaz.
    // Güvenliği imza sağlar: içerik değiştirilirse imza tutmaz.
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

        public AccessToken CreateToken(Guid userId, string email)
        {
            var now = DateTime.UtcNow;
            var expiration = now.AddMinutes(_tokenOptions.AccessTokenExpirationMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _tokenOptions.Issuer,
                Audience = _tokenOptions.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = expiration,
                SigningCredentials = _signingCredentials,
                // Claim = token'ın taşıdığı kullanıcı bilgisi. İstek geldiğinde ICurrentUser bunları okur.
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    // jti: token'a özgü benzersiz Id (ileride iptal/blacklist için kullanılabilir)
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                ])
            };

            return new AccessToken
            {
                Token = _tokenHandler.CreateToken(descriptor),
                Expiration = expiration
            };
        }
    }
}
