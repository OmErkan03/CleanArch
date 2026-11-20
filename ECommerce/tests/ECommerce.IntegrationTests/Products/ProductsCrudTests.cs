using System.Net;
using System.Net.Http.Json;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Products;

// AC-3…AC-6 — ürün yaşam döngüsü, gözlemlenebilir sonuçla (durum + başlık + gövde + sonraki okuma).
public class ProductsCrudTests(ApiFactory factory)
{
    private readonly TestUsers _users = new(factory);

    // AC-3
    [Fact]
    public async Task Create_ReturnsCreatedWithLocation_AndPersistsValues()
    {
        using var admin = await _users.CreateAdminAsync();
        var name = ProductApi.UniqueName("kalem");

        var response = await admin.CreateAsync(new { name, amount = 12.34m, currency = "TRY", stock = 15 });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.ShouldNotBe(Guid.Empty);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.AbsolutePath.ShouldBe($"{ProductApi.Route}/{id}", StringCompareShould.IgnoreCase);

        var stored = await admin.GetOkAsync(id);
        stored.ShouldBe(new ProductDto(id, name, 12.34m, "TRY", 15));
    }

    // AC-3: Location başlığı gerçekten okunabilir bir adresi gösterir.
    [Fact]
    public async Task Create_LocationHeader_ResolvesToTheNewProduct()
    {
        using var admin = await _users.CreateAdminAsync();
        var response = await admin.CreateAsync(ProductApi.NewProduct());
        var id = await response.Content.ReadFromJsonAsync<Guid>();

        var followed = await admin.GetAsync(response.Headers.Location!);

        followed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await followed.Content.ReadFromJsonAsync<ProductDto>())!.Id.ShouldBe(id);
    }

    // AC-4
    [Fact]
    public async Task List_ContainsCreatedProducts()
    {
        using var admin = await _users.CreateAdminAsync();
        var first = await admin.CreateOkAsync();
        var second = await admin.CreateOkAsync();

        var list = await (await admin.ListAsync()).Content.ReadFromJsonAsync<List<ProductDto>>();

        list.ShouldNotBeNull();
        list.Select(p => p.Id).ShouldContain(first);
        list.Select(p => p.Id).ShouldContain(second);
        list.Single(p => p.Id == first).ShouldBe(await admin.GetOkAsync(first));
    }

    // AC-4
    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        using var admin = await _users.CreateAdminAsync();
        await (await admin.GetAsync(Guid.NewGuid())).ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    // AC-5
    [Fact]
    public async Task Update_PersistsNewValues()
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync(ProductApi.NewProduct(amount: 10m, currency: "TRY", stock: 1));
        var newName = ProductApi.UniqueName("updated");

        var response = await admin.UpdateAsync(id, new { name = newName, amount = 20.5m, currency = "USD", stock = 3 });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetOkAsync(id)).ShouldBe(new ProductDto(id, newName, 20.5m, "USD", 3));
    }

    // AC-5 / docs/security.md: route id, gövdedeki id'ye üstündür.
    [Fact]
    public async Task Update_BodyIdIgnored_RouteIdWins()
    {
        using var admin = await _users.CreateAdminAsync();
        var target = await admin.CreateOkAsync(ProductApi.NewProduct(amount: 1m));
        var bystander = await admin.CreateOkAsync(ProductApi.NewProduct(amount: 2m));
        var bystanderBefore = await admin.GetOkAsync(bystander);

        var response = await admin.UpdateAsync(target, new { id = bystander, name = ProductApi.UniqueName(), amount = 3m, currency = "TRY", stock = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetOkAsync(target)).Amount.ShouldBe(3m);
        (await admin.GetOkAsync(bystander)).ShouldBe(bystanderBefore);
    }

    // AC-5
    [Fact]
    public async Task Update_Unknown_Returns404_AndCreatesNothing()
    {
        using var admin = await _users.CreateAdminAsync();
        var unknown = Guid.NewGuid();
        var name = ProductApi.UniqueName();

        await (await admin.UpdateAsync(unknown, ProductApi.NewProduct(name))).ShouldBeProblemAsync(HttpStatusCode.NotFound);

        (await admin.GetAsync(unknown)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await (await admin.ListAsync()).Content.ReadAsStringAsync()).ShouldNotContain(name);
    }

    // AC-6
    [Fact]
    public async Task Delete_ThenGet_Returns404()
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();

        (await admin.DeleteAsync(id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await (await admin.GetAsync(id)).ShouldBeProblemAsync(HttpStatusCode.NotFound);
        var list = await (await admin.ListAsync()).Content.ReadFromJsonAsync<List<ProductDto>>();
        list!.Select(p => p.Id).ShouldNotContain(id);
    }

    // AC-6
    [Fact]
    public async Task Delete_Twice_Returns404()
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        (await admin.DeleteAsync(id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await (await admin.DeleteAsync(id)).ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    // AC-6
    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        using var admin = await _users.CreateAdminAsync();
        await (await admin.DeleteAsync(Guid.NewGuid())).ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }
}
