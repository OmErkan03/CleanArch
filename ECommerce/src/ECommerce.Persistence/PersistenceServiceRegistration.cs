using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using ECommerce.Persistence.Repositories;
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

            // UseNpgsql => EF Core'a veritabanının PostgreSQL olduğunu söyler.
            services.AddDbContext<UludagDbContext>(options => options.UseNpgsql(connectionString));

            // Repository'ler: DbContext ile aynı ömürde (Scoped) olmalı, yani her HTTP isteğinde bir tane.
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
