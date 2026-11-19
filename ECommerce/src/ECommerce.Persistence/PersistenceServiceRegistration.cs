using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence
{
    // Persistence katmanının (veritabanı) tüm kayıtlarını tek noktadan DI'a ekler.
    public static class PersistenceServiceRegistration
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Connection string appsettings.json / appsettings.Development.json'daki "ConnectionStrings:PostgreSQL" alanından okunur.
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("'ConnectionStrings:PostgreSQL' ayarı bulunamadı.");

            services.AddDbContext<UludagDbContext>(options => options
                // UseNpgsql => EF Core'a veritabanının PostgreSQL olduğunu söyler.
                .UseNpgsql(connectionString)
                // Başlangıç verileri (bkz. Seeds/ProductSeed.cs). EF, migration/EnsureCreated sonrasında bunları çalıştırır.
                // EF her iki sürümün de tanımlanmasını önerir: senkron/asenkron hangi API çağrılırsa o kullanılır.
                .UseSeeding((context, _) => ProductSeed.Seed(context))
                .UseAsyncSeeding((context, _, cancellationToken) => ProductSeed.SeedAsync(context, cancellationToken)));

            // Repository'ler: DbContext ile aynı ömürde (Scoped) olmalı, yani her HTTP isteğinde bir tane.
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOperationClaimRepository, OperationClaimRepository>();

            return services;
        }
    }
}
