using System.Net;
using System.Text.Json;

namespace ECommerce.IntegrationTests.Infrastructure;

// docs/security.md: 500 yanıtı istemciye istisna detayı (mesaj, stack trace) sızdırmaz (spec 0002, AC-10).
// Her test istemcisine takılır; ihlal görürse isteği yapan testi başarısız kılar.
public sealed class ServerErrorGuard : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.InternalServerError)
            return response;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        body.ShouldNotContain("   at ", customMessage: $"500 on {request.Method} {request.RequestUri} leaks a stack trace");
        body.ShouldNotContain("Exception", customMessage: $"500 on {request.Method} {request.RequestUri} leaks an exception type");
        if (body.Length > 0)
        {
            using var json = JsonDocument.Parse(body);
            var hasDetail = json.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind != JsonValueKind.Null;
            hasDetail.ShouldBeFalse($"500 on {request.Method} {request.RequestUri} carries a detail: {body}");
        }

        // ReadAsStringAsync içeriği tamponlar: test aynı içeriği (ve özgün başlıklarını) yeniden okuyabilir.
        return response;
    }
}
