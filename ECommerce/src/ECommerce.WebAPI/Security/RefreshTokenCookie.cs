namespace ECommerce.WebAPI.Security
{
    // Refresh token'ın taşındığı cookie.
    //
    //   HttpOnly        : JavaScript okuyamaz => XSS ile çalınamaz.
    //   Secure          : yalnızca HTTPS ile gönderilir (tarayıcılar http://localhost'u güvenli sayar, geliştirmede de çalışır).
    //   SameSite=Strict : başka bir siteden tetiklenen isteklerde gönderilmez => CSRF ile /refresh çağrılamaz.
    //   Path=/api/auth  : yalnızca auth uç noktalarına gider; diğer tüm isteklerde ağ üzerinde taşınmaz.
    //   Expires         : refresh token ailesinin mutlak bitişi (login + 3 gün).
    public static class RefreshTokenCookie
    {
        public const string Name = "refreshToken";
        private const string Path = "/api/auth";

        public static void Append(HttpResponse response, string refreshToken, DateTime expiresAt) =>
            response.Cookies.Append(Name, refreshToken, CreateOptions(expiresAt));

        // Cookie'yi istemciden siler. OnStarting ile eklenir: istek sonradan hata verirse (401...) exception handler
        // yanıt başlıklarını temizler; OnStarting yanıt gönderilmeden hemen önce çalıştığı için silme talimatı kaybolmaz.
        public static void Delete(HttpResponse response) =>
            response.OnStarting(() =>
            {
                response.Cookies.Delete(Name, CreateOptions(expiresAt: null));
                return Task.CompletedTask;
            });

        public static string? Read(HttpRequest request) => request.Cookies[Name];

        private static CookieOptions CreateOptions(DateTime? expiresAt) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expiresAt is null ? null : new DateTimeOffset(expiresAt.Value, TimeSpan.Zero),
            IsEssential = true
        };
    }
}
