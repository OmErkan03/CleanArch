using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Uludag.Core.RateLimiting;

namespace ECommerce.WebAPI.RateLimiting
{
    // Her HTTP isteğini leaky bucket rate limiter'dan geçirir (bkz. Uludag.Core => RedisLeakyBucketRateLimiter).
    //
    // Kova kime ait?
    //   - Oturum açmış kullanıcı => "user:{userId}"  (aynı kullanıcı farklı cihaz/IP'lerden gelse de tek kova)
    //   - Anonim istek (login, register, MFA, geçersiz token) => "ip:{ip}"
    //   Bu yüzden middleware UseAuthentication'dan SONRA çalışmalıdır (HttpContext.User dolu olsun).
    //
    // Tüm uç noktalar TEK kovayı paylaşır: uç nokta başına ayrı kova olsaydı istemci isteklerini farklı uç noktalara
    // dağıtarak sınırı katlayabilirdi. 401/403/404 dönen istekler de sayılır (şifre/kod deneme saldırılarını yavaşlatır).
    public class LeakyBucketRateLimitingMiddleware(
        RequestDelegate next,
        IRateLimiter rateLimiter,
        LeakyBucketOptions options,
        IProblemDetailsService problemDetailsService)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            if (IsExcluded(context.Request.Path))
            {
                await next(context);
                return;
            }

            var result = await rateLimiter.TryAcquireAsync(ResolvePartitionKey(context), context.RequestAborted);

            // İstemci kalan hakkını görebilsin (yaygın kullanılan, standart dışı başlıklar).
            // Doğrudan Headers'a yazmak yetmez: istek sonradan hata verirse (401/403/404...) UseExceptionHandler yanıtı
            // temizler ve başlıklar kaybolur. OnStarting, yanıt gönderilmeden hemen önce çalışır ve temizlemeden etkilenmez.
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString(CultureInfo.InvariantCulture);
                context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString(CultureInfo.InvariantCulture);
                return Task.CompletedTask;
            });

            if (result.IsAllowed)
            {
                await next(context);
                return;
            }

            // Retry-After: saniye cinsinden, yukarı yuvarlanır (erken denerse yine reddedilmesin).
            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(result.RetryAfter.TotalSeconds));
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Çok fazla istek",
                    Detail = $"İstek sınırı aşıldı. {retryAfterSeconds} saniye sonra tekrar deneyin."
                }
            });
        }

        private bool IsExcluded(PathString path) =>
            options.ExcludedPaths.Any(excluded => path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));

        private static string ResolvePartitionKey(HttpContext context)
        {
            // Token geçerli ve Redis'te oturumu varsa SessionTokenValidator "sub" claim'ini eklemiştir.
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (!string.IsNullOrEmpty(userId))
                    return $"user:{userId}";
            }

            // Proxy desteği açıksa (ReverseProxyOptions) RemoteIpAddress, UseForwardedHeaders tarafından
            // güvenilen proxy'nin X-Forwarded-For başlığındaki gerçek istemci IP'siyle değiştirilmiş olur.
            var ip = context.Connection.RemoteIpAddress;
            if (ip is null)
                return "ip:unknown";

            // "::ffff:127.0.0.1" ile "127.0.0.1" aynı istemcidir; tek kova olsun.
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            return $"ip:{ip}";
        }
    }

    public static class LeakyBucketRateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLeakyBucketRateLimiting(this IApplicationBuilder app) =>
            app.UseMiddleware<LeakyBucketRateLimitingMiddleware>();
    }
}
