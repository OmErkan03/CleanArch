using System.Text.Json.Serialization;
using ECommerce.Application.Features.Auth.Services;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Dtos
{
    // JSON'da sayı (0,1,2) yerine adıyla ("MfaSetupRequired") görünsün; istemci okunabilir değere göre dallanır.
    [JsonConverter(typeof(JsonStringEnumConverter<LoginStatus>))]
    public enum LoginStatus
    {
        // Giriş tamamlandı, AccessToken dolu.
        Succeeded,

        // Şifre doğru ama kullanıcının MFA'sı yok => MfaToken ile /api/auth/mfa/setup ve /api/auth/mfa/setup/confirm çağrılmalı.
        MfaSetupRequired,

        // Şifre doğru, MFA kurulu => MfaToken ve authenticator kodu ile /api/auth/mfa/verify çağrılmalı.
        MfaVerificationRequired
    }

    // Login ve MFA adımlarının ortak cevabı. Status'a göre ya AccessToken ya da MfaToken doludur.
    public class LoginResponse
    {
        public LoginStatus Status { get; init; }

        public AccessToken? AccessToken { get; init; }

        // JWT DEĞİLDİR: yalnızca MFA endpoint'lerinde geçerli, kısa ömürlü, tek kullanımlık anahtar.
        public string? MfaToken { get; init; }
        public DateTime? MfaTokenExpiration { get; init; }

        public string? Message { get; init; }

        // Refresh token yanıt GÖVDESİNE yazılmaz (JsonIgnore): WebAPI bunu HttpOnly cookie olarak gönderir,
        // böylece tarayıcıdaki JavaScript (ve olası bir XSS) onu okuyamaz.
        [JsonIgnore]
        public string? RefreshToken { get; init; }

        [JsonIgnore]
        public DateTime? RefreshTokenExpiration { get; init; }

        public static LoginResponse Succeeded(AuthTokens tokens) =>
            new()
            {
                Status = LoginStatus.Succeeded,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiration = tokens.RefreshTokenExpiration
            };
    }
}
