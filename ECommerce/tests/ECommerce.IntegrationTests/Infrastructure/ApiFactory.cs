using System.Security.Cryptography;
using ECommerce.Persistence.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace ECommerce.IntegrationTests.Infrastructure;

// API'yi bellekte (TestServer) çalıştırır; veritabanı ve Redis bu test çalıştırması için açılan
// GERÇEK container'lardır. Geliştiricinin docker-compose verilerine ve repodaki appsettings
// secret'larına dokunulmaz: ortam "Testing", anahtarlar her çalıştırmada yeniden üretilir.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // Uygulama açılışta migration çalıştırmaz; şema her çalıştırmada boş veritabanına uygulanır.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting: Program.cs ayarları builder aşamasında (Build'den önce) okuduğu için
        // ConfigureAppConfiguration yerine bu kullanılır.
        builder.UseSetting("ConnectionStrings:PostgreSQL", _postgres.GetConnectionString());
        builder.UseSetting("RedisOptions:ConnectionString", _redis.GetConnectionString());
        builder.UseSetting("RedisOptions:InstanceName", "integration-tests:");

        builder.UseSetting("TokenOptions:Issuer", "integration-tests");
        builder.UseSetting("TokenOptions:Audience", "integration-tests");
        builder.UseSetting("TokenOptions:SecurityKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("TokenOptions:AccessTokenExpirationMinutes", "60");
        builder.UseSetting("TokenOptions:RefreshTokenExpirationDays", "3");

        builder.UseSetting("MfaOptions:Issuer", "integration-tests");
        builder.UseSetting("MfaOptions:SecretEncryptionKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        builder.UseSetting("LeakyBucketOptions:Capacity", "10");
        builder.UseSetting("LeakyBucketOptions:LeakIntervalSeconds", "12");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
