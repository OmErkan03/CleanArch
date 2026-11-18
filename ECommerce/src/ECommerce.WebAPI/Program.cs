using ECommerce.Application;
using ECommerce.Persistence;
using ECommerce.WebAPI.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAuthorization();

app.MapControllers();

app.Run();
