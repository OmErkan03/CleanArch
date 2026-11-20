using System.Net;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Products;

// AC-1 / BR-1: token'sız hiçbir ürün işlemi yapılamaz — 401 + ProblemDetails, veri değişmez.
public class ProductsAuthenticationTests(ApiFactory factory)
{
    private readonly TestUsers _users = new(factory);

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        using var anonymous = factory.CreateGuardedClient();
        await (await anonymous.ListAsync()).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        using var anonymous = factory.CreateGuardedClient();

        await (await anonymous.GetAsync(id)).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401_AndCreatesNothing()
    {
        using var admin = await _users.CreateAdminAsync();
        var name = ProductApi.UniqueName();
        using var anonymous = factory.CreateGuardedClient();

        await (await anonymous.CreateAsync(ProductApi.NewProduct(name))).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);

        var list = await (await admin.ListAsync()).Content.ReadAsStringAsync();
        list.ShouldNotContain(name);
    }

    [Fact]
    public async Task Update_WithoutToken_Returns401_AndChangesNothing()
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync(ProductApi.NewProduct(amount: 10m));
        using var anonymous = factory.CreateGuardedClient();

        await (await anonymous.UpdateAsync(id, ProductApi.NewProduct(amount: 99m))).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);

        (await admin.GetOkAsync(id)).Amount.ShouldBe(10m);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401_AndDeletesNothing()
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        using var anonymous = factory.CreateGuardedClient();

        await (await anonymous.DeleteAsync(id)).ShouldBeProblemAsync(HttpStatusCode.Unauthorized);

        (await admin.GetAsync(id)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
