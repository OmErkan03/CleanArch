using ECommerce.Application.Features.Auth.Services;
using Microsoft.Extensions.DependencyInjection;
using Uludag.Core.Mediation.Behaviors;
using Uludag.Core.Mediation.Registration;

namespace ECommerce.Application
{
    // Application katmanının ihtiyaç duyduğu tüm servisleri tek noktadan DI'a kaydeder.
    // Program.cs yalnızca "builder.Services.AddApplicationServices();" çağırır,
    // içeride neyin kaydedildiğini bilmek zorunda kalmaz.
    public static class ApplicationServiceRegistration
    {
        // "this IServiceCollection services" => extension method:
        // builder.Services.AddApplicationServices() şeklinde çağrılabilmesini sağlar.
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Mediator: bu assembly'deki tüm handler'ları tarar, IMediator/ISender/IPublisher'ı kaydeder.
            var descriptors = MediatorRegistrar.CreateDescriptors(cfg => cfg
                .RegisterServicesFromAssemblyContaining<AssemblyReference>()
                .AddBehavior(typeof(LoggingBehavior<,>))
                // Oturum açılmamışsa tüm istekleri handler'a ulaşmadan durdurur (IAllowAnonymousRequest hariç).
                .AddBehavior(typeof(AuthenticationBehavior<,>))
                // Kimliği doğrulanan kullanıcının, ISecuredRequest.Roles'daki claim'lerden birine sahip olmasını şart koşar (403).
                .AddBehavior(typeof(AuthorizationBehavior<,>)));

            foreach (var descriptor in descriptors)
            {
                services.Add(new ServiceDescriptor(
                    descriptor.ServiceType,
                    descriptor.ImplementationType,
                    (ServiceLifetime)descriptor.Lifetime));
            }

            // BaseBusinessRules DI'a kaydeder
            var businessRulesTypes = typeof(AssemblyReference).Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseBusinessRules)));
            foreach (var type in businessRulesTypes)
            {
                services.AddScoped(type, type);
            }

            // Şifre + MFA doğrulanmış kullanıcı için Redis oturumu açıp JWT üretir (MFA komutları kullanır).
            services.AddScoped<AccessTokenService>();

            // İleride Application'a ait diğer kayıtlar da buraya eklenir (mapper, validator vb.)

            // Zincirleme çağrı yapılabilsin diye aynı koleksiyonu geri döndürürüz:
            // builder.Services.AddApplicationServices().AddPersistenceServices();
            return services;
        }
    }
}
