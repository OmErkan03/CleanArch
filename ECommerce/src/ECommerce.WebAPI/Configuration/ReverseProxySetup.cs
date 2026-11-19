using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ECommerce.WebAPI.Configuration
{
    // appsettings'teki "ReverseProxyOptions" bölümünün karşılığı.
    //
    // API bir reverse proxy / load balancer (Nginx, IIS ARR, Azure Front Door...) arkasındaysa bağlantı proxy'den gelir;
    // RemoteIpAddress her istekte PROXY'nin IP'si olur ve tüm anonim istemciler aynı rate limit kovasını paylaşır.
    // Proxy, gerçek istemci IP'sini X-Forwarded-For başlığında iletir.
    //
    // GÜVENLİK: X-Forwarded-For'u herkes gönderebilir. Yalnızca GÜVENİLEN proxy adreslerinden gelen başlık dikkate alınır;
    // aksi halde saldırgan her istekte sahte bir IP yazarak rate limit'i atlatabilirdi.
    public class ReverseProxyOptions
    {
        public const string SectionName = "ReverseProxyOptions";

        // Kapalıyken X-Forwarded-For tamamen yok sayılır (proxy yokken doğru ve güvenli olan budur).
        public bool Enabled { get; set; }

        // Güvenilen proxy IP adresleri. Ör: "10.0.0.5"
        public List<string> KnownProxies { get; set; } = [];

        // Güvenilen proxy ağları (CIDR). Ör: "10.0.0.0/24"
        public List<string> KnownNetworks { get; set; } = [];

        // X-Forwarded-For'da sağdan kaç adresin işleneceği = arka arkaya kaç proxy olduğu.
        public int ForwardLimit { get; set; } = 1;
    }

    public static class ReverseProxySetup
    {
        public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(ReverseProxyOptions.SectionName).Get<ReverseProxyOptions>() ?? new ReverseProxyOptions();
            services.AddSingleton(options);

            if (!options.Enabled)
                return services;

            if (options.KnownProxies.Count == 0 && options.KnownNetworks.Count == 0)
                throw new InvalidOperationException(
                    $"'{ReverseProxyOptions.SectionName}' açık ama hiç güvenilen proxy (KnownProxies / KnownNetworks) tanımlanmamış.");

            services.Configure<ForwardedHeadersOptions>(forwarded =>
            {
                forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                forwarded.ForwardLimit = options.ForwardLimit;

                // Varsayılan güvenilen liste (loopback) temizlenir; yalnızca ayarlarda yazanlara güvenilir.
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();

                foreach (var proxy in options.KnownProxies)
                    forwarded.KnownProxies.Add(IPAddress.Parse(proxy));

                foreach (var network in options.KnownNetworks)
                    forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            });

            return services;
        }

        // Pipeline'da IP'yi okuyan her şeyden (rate limiting dahil) ÖNCE çağrılmalıdır.
        public static IApplicationBuilder UseReverseProxySupport(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<ReverseProxyOptions>();
            return options.Enabled ? app.UseForwardedHeaders() : app;
        }
    }
}
