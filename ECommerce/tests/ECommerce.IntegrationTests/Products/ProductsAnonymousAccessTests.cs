using System.Net;
using System.Text.Json;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Products;

// BR-1: IAllowAnonymousRequest olmayan her istek kimlik doğrulaması ister (secure by default).
public class ProductsAnonymousAccessTests(ApiFactory factory)
{
    [Fact]
    public async Task GetAll_WithoutToken_Returns401ProblemDetails()
    {
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/api/products", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(401);
    }
}
