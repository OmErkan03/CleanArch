using ECommerce.Application;
using ECommerce.Persistence;
using ECommerce.WebAPI.ExceptionHandling;
using ECommerce.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Uludag.Core.Security;
using Uludag.Core.Security.Constants;
using Uludag.Core.Security.JWT;

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
            // Operation claim'ler token'da "role" adıyla taşınır (bkz. JwtHelper).
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
    });

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
app.UseAuthorization();

app.MapControllers();

app.Run();
