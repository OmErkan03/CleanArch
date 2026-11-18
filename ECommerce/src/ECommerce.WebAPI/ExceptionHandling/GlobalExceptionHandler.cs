using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Exceptions;

namespace ECommerce.WebAPI.ExceptionHandling
{
    // Handler'lardaki iş kuralı sınıfları (BaseBusinessRules) kural ihlalinde exception fırlatır.
    // Bu sınıf o exception'ları yakalayıp uygun HTTP durum koduna ve ProblemDetails (RFC 7807) gövdesine çevirir.
    // Böylece ne handler'larda ne de controller'larda if-else ile durum kontrolü yapmak gerekir.
    public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
        : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title) = exception switch
            {
                UnauthorizedException => (StatusCodes.Status401Unauthorized, "Yetkisiz erişim"),
                NotFoundException => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
                BusinessException => (StatusCodes.Status400BadRequest, "İş kuralı ihlali"),
                _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu")
            };

            // Beklenmeyen hatanın detayı (stack trace, iç mesaj) istemciye sızdırılmaz, yalnızca loglanır.
            var detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message;
            if (statusCode == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "İşlenmeyen hata");

            httpContext.Response.StatusCode = statusCode;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail
                }
            });
        }
    }
}
