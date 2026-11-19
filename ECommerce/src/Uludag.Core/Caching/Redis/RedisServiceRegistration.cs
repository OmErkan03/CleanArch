using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Uludag.Core.Security.RefreshTokens;
using Uludag.Core.Security.Sessions;

namespace Uludag.Core.Caching.Redis
{
    public static class RedisServiceRegistration
    {
        // builder.Services.AddRedisCache(builder.Configuration)
        // => Redis bağlantısını, ICacheService'i ve Redis üzerinde çalışan oturum deposunu (ISessionStore) kaydeder.
        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                ?? throw new InvalidOperationException($"'{RedisOptions.SectionName}' ayarı bulunamadı.");

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new InvalidOperationException($"'{RedisOptions.SectionName}:ConnectionString' boş olamaz.");

            services.AddSingleton(options);

            // ConnectionMultiplexer pahalıdır ve thread-safe'tir => uygulama boyunca TEK instance (Singleton) kullanılır.
            // Factory ile kaydedildiği için bağlantı ilk ihtiyaçta açılır.
            // "abortConnect=false" ile Redis o an kapalıysa uygulama çökmez, arka planda yeniden bağlanmayı dener.
            // Bağlantı cümlesinde "serviceName" varsa Connect sentinel'lere bağlanır ve master'a giden bağlantıyı döndürür
            // (bkz. RedisOptions.ConnectionString). Uygulama kodu tek sunucu / sentinel farkını bilmez.
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options.ConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddSingleton<ISessionStore, CacheSessionStore>();
            services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();

            return services;
        }
    }
}
