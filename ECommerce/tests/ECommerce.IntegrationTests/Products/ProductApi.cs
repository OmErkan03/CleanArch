using System.Net.Http.Json;

namespace ECommerce.IntegrationTests.Products;

// Ürün API'sinin HTTP sözleşmesi (beklenen biçim, koddan değil sözleşmeden):
//   GET /api/products · GET /api/products/{id} · POST /api/products · PUT /api/products/{id} · DELETE /api/products/{id}
public sealed record ProductDto(Guid Id, string? Name, decimal Amount, string Currency, int Stock);

public static class ProductApi
{
    public const string Route = "/api/products";

    // Paralel testler aynı veritabanını paylaşır: her ürün adı benzersizdir (spec 0002, AC-13).
    public static string UniqueName(string prefix = "product") => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 33, 100)];

    public static object NewProduct(string? name = null, decimal amount = 25.50m, string currency = "TRY", int stock = 7)
        => new { name = name ?? UniqueName(), amount, currency, stock };

    public static Task<HttpResponseMessage> CreateAsync(this HttpClient client, object body) => client.PostAsJsonAsync(Route, body);

    public static async Task<Guid> CreateOkAsync(this HttpClient client, object? body = null)
    {
        var response = await client.CreateAsync(body ?? NewProduct());
        response.IsSuccessStatusCode.ShouldBeTrue($"create: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public static Task<HttpResponseMessage> GetAsync(this HttpClient client, Guid id) => client.GetAsync($"{Route}/{id}");

    public static async Task<ProductDto> GetOkAsync(this HttpClient client, Guid id)
    {
        var response = await client.GetAsync(id);
        response.IsSuccessStatusCode.ShouldBeTrue($"get {id}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    public static Task<HttpResponseMessage> ListAsync(this HttpClient client) => client.GetAsync(Route);

    public static Task<HttpResponseMessage> UpdateAsync(this HttpClient client, Guid id, object body) => client.PutAsJsonAsync($"{Route}/{id}", body);

    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient client, Guid id) => client.DeleteAsync($"{Route}/{id}");
}
