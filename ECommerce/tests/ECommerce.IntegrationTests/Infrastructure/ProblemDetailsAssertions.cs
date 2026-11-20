using System.Net;
using System.Text.Json;

namespace ECommerce.IntegrationTests.Infrastructure;

public static class ProblemDetailsAssertions
{
    // Hata sözleşmesi (docs/conventions.md, Error handling): durum kodu + RFC 7807 ProblemDetails gövdesi.
    public static async Task ShouldBeProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(expected, $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} → {body}");
        response.Content.Headers.ContentType.ShouldNotBeNull("a ProblemDetails response carries a Content-Type");
        response.Content.Headers.ContentType.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe((int)expected);
    }
}
