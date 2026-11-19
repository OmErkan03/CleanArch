using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Uludag.Core.RateLimiting
{
    public static class RateLimitingServiceRegistration
    {
        // builder.Services.AddLeakyBucketRateLimiting(builder.Configuration)
        // Kovalar Redis'te tutulduğu için AddRedisCache'in de çağrılmış olması gerekir (IConnectionMultiplexer, RedisOptions).
        public static IServiceCollection AddLeakyBucketRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(LeakyBucketOptions.SectionName).Get<LeakyBucketOptions>()
                ?? throw new InvalidOperationException($"'{LeakyBucketOptions.SectionName}' ayarı bulunamadı.");

            services.AddSingleton(options);
            services.AddSingleton<IRateLimiter, RedisLeakyBucketRateLimiter>();

            return services;
        }
    }
}
