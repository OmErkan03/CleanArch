using ECommerce.Application;
using ECommerce.Persistence;
using ECommerce.WebAPI.Configuration;
using ECommerce.WebAPI.ExceptionHandling;
using ECommerce.WebAPI.RateLimiting;
using ECommerce.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Uludag.Core.Caching.Redis;
using Uludag.Core.RateLimiting;
using Uludag.Core.Security;
using Uludag.Core.Security.Constants;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.MultiFactor;

var builder = WebApplication.CreateBuilder(args);

// ---------- JWT ----------
// TokenOptions appsettings'teki "TokenOptions" bölümünden okunur.
var tokenOptions = builder.Configuration.GetSection(TokenOptions.SectionName).Get<TokenOptions>()
    ?? throw new InvalidOperationException($"'{TokenOptions.SectionName}' ayarı bulunamadı.");

// Token ÜRETİMİ (Login): JwtHelper ayarları değişmediği için tek instance (Singleton) yeterli.
builder.Services.AddSingleton(tokenOptions);
builder.Services.AddSingleton<ITokenHelper, JwtHelper>();

// Token DOĞRULAMASI: her istekte "Authorization: Bearer ..." başlığı okunur.
// Geçerliyse HttpContext.User doldurulur; geçersiz/yoksa kullanıcı anonim kalır
// (isteği reddetmek AuthenticationBehavior'ın işidir).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claim isimleri (sub, email) olduğu gibi kalsın; .NET'in uzun ClaimTypes URI'lerine çevrilmesin.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Operation claim'ler "role" adıyla taşınır (Redis'teki oturumdan eklenir, bkz. SessionTokenValidator).
            RoleClaimType = SecurityClaimTypes.OperationClaim,
            ValidateIssuer = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = tokenOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // JwtHelper ile AYNI anahtar => imzası tutmayan (değiştirilmiş / başka anahtarla üretilmiş) token reddedilir.
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
            // Varsayılan 5 dk tolerans yerine süresi dolan token hemen geçersiz olsun.
            ClockSkew = TimeSpan.Zero
        };

        // Token yalnızca oturum anahtarını taşır. İmza doğrulandıktan sonra kullanıcı bilgileri (sub, email, role) Redis'ten okunur.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = SessionTokenValidator.OnTokenValidated
        };
    });

// ---------- Redis ----------
// Oturumlar (UserSession) Redis'te tutulur. ICacheService ve ISessionStore'u kaydeder.
builder.Services.AddRedisCache(builder.Configuration);

// ---------- MFA ----------
// TOTP (authenticator uygulaması) ile ikinci faktör. Yarım kalan girişler (MFA challenge) Redis'te tutulur.
builder.Services.AddMultiFactorAuthentication(builder.Configuration);

// ---------- Rate limiting ----------
// Leaky bucket: kullanıcı (yoksa IP) başına kapasite 10, her 12 sn'de 1 damla. Kovalar Redis'te tutulur.
builder.Services.AddLeakyBucketRateLimiting(builder.Configuration);

// Reverse proxy arkasında gerçek istemci IP'si (X-Forwarded-For). Varsayılan: kapalı (bkz. ReverseProxyOptions).
builder.Services.AddReverseProxySupport(builder.Configuration);

// AuthenticationBehavior isteği yapan kullanıcıyı ICurrentUser üzerinden öğrenir.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Add services to the container.

builder.Services.AddControllers();

// İş kuralı exception'larını (NotFoundException => 404, BusinessException => 400) ProblemDetails yanıtına çevirir.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Application katmanının tüm kayıtları (mediator, handler'lar, behavior'lar)
builder.Services.AddApplicationServices();

// Persistence katmanı: PostgreSQL bağlantısı ve DbContext
builder.Services.AddPersistenceServices(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline'ın en başında olmalı ki sonraki tüm katmanlarda fırlatılan exception'ları yakalayabilsin.
app.UseExceptionHandler();

// İstemci IP'sini okuyan her şeyden önce: güvenilen proxy'den gelen X-Forwarded-For'u RemoteIpAddress'e yazar (açıksa).
app.UseReverseProxySupport();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // /openapi/v1.json => API'nin dokümanını (endpoint'ler, request/response şemaları) üretir.
    app.MapOpenApi();

    // /swagger => Yukarıdaki dokümanı okuyup tarayıcıda test edilebilir bir arayüz olarak gösterir.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ECommerce API v1");
    });
}

app.UseHttpsRedirection();

// Token'ı doğrulayıp HttpContext.User'ı dolduran middleware. UseAuthorization'dan ÖNCE olmalı.
app.UseAuthentication();

// Authentication'dan SONRA: kova, oturum açmış kullanıcıya göre (yoksa IP'ye göre) seçilir.
// Controller'lardan ÖNCE: sınırı aşan istek hiçbir iş yapılmadan 429 ile döner.
app.UseLeakyBucketRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();
