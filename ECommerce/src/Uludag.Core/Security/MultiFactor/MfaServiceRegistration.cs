using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Uludag.Core.Security.MultiFactor
{
    public static class MfaServiceRegistration
    {
        // builder.Services.AddMultiFactorAuthentication(builder.Configuration)
        // MFA challenge'ları Redis'te tutulduğu için AddRedisCache'in de çağrılmış olması gerekir (ICacheService).
        public static IServiceCollection AddMultiFactorAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(MfaOptions.SectionName).Get<MfaOptions>()
                ?? throw new InvalidOperationException($"'{MfaOptions.SectionName}' ayarı bulunamadı.");

            services.AddSingleton(options);
            services.AddSingleton<ITotpHelper, TotpHelper>();
            services.AddSingleton<IMfaChallengeStore, CacheMfaChallengeStore>();

            return services;
        }
    }
}
