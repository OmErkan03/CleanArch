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

        // Rate limit bu testlerin konusu değil (spec 0002, clarify 4): tüm istekler tek IP'den
        // ("ip:unknown") geldiği için kapasite yükseltilir. Yalnızca test konfigürasyonu; kod değişmez.
        builder.UseSetting("LeakyBucketOptions:Capacity", "100000");
        builder.UseSetting("LeakyBucketOptions:LeakIntervalSeconds", "1");
    }

    // Test istemcisi: her yanıt ServerErrorGuard'dan geçer (500'de istisna detayı sızmamalı, AC-10).
    public HttpClient CreateGuardedClient() => CreateDefaultClient(new ServerErrorGuard());

    // YALNIZCA arrange: sistemde seed edilmiş admin yok ve claim API'si Admin ister. İlk admin'i
    // uygulamanın kendi DbContext'iyle oluşturur; diğer tüm claim'ler gerçek API ile verilir.
    public async Task GrantClaimDirectlyAsync(Guid userId, string claimName)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
        var user = await db.Users.Include(u => u.OperationClaims).SingleAsync(u => u.Id == userId);
        var claim = await db.OperationClaims.SingleAsync(c => c.Name == claimName);
        user.OperationClaims.Add(claim);
        await db.SaveChangesAsync();
    }

    public async Task<int> ClaimIdAsync(string claimName)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UludagDbContext>();
        return (await db.OperationClaims.AsNoTracking().SingleAsync(c => c.Name == claimName)).Id;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
