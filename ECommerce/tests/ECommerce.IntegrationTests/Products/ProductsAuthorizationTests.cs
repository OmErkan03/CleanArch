using System.Net;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Products;

// AC-2 / BR-2, BR-3 — yetki matrisi (plan 0002). Beklenti docs/domain.md'den:
//   Read → list/get · Create|Write → create · Update|Write → update · Delete|Write → delete ·
//   Product.Admin ve Admin → hepsi. Reddedilen kullanıcı 403 + ProblemDetails alır, veri değişmez.
// Claim adları sözleşmedir: koddaki sabitlerden değil, dokümandan yazılır.
public class ProductsAuthorizationTests(ApiFactory factory)
{
    private readonly TestUsers _users = new(factory);

    [Theory]
    [InlineData("Product.Read")]
    [InlineData("Product.Admin")]
    [InlineData("Admin")]
    public async Task List_WithAcceptedClaim_Returns200(string claim)
    {
        using var user = await _users.CreateWithClaimsAsync(claim);
        (await user.ListAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Order.Admin")]
    public async Task List_WithRefusedClaim_Returns403(string claim)
    {
        using var user = await _users.CreateWithClaimsAsync(claim);
        await (await user.ListAsync()).ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Product.Read")]
    [InlineData("Product.Admin")]
    [InlineData("Admin")]
    public async Task GetById_WithAcceptedClaim_Returns200(string claim)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        using var user = await _users.CreateWithClaimsAsync(claim);

        (await user.GetOkAsync(id)).Id.ShouldBe(id);
    }

    [Theory]
    [InlineData("Order.Admin")]
    public async Task GetById_WithRefusedClaim_Returns403(string claim)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        using var user = await _users.CreateWithClaimsAsync(claim);

        await (await user.GetAsync(id)).ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Product.Create")]
    [InlineData("Product.Write")]
    [InlineData("Product.Admin")]
    [InlineData("Admin")]
    public async Task Create_WithAcceptedClaim_Returns201(string claim)
    {
        using var user = await _users.CreateWithClaimsAsync(claim);
        using var admin = await _users.CreateAdminAsync();

        var response = await user.CreateAsync(ProductApi.NewProduct());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<Guid>(response.Content);
        (await admin.GetAsync(id)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Product.Read")]
    public async Task Create_WithRefusedClaim_Returns403_AndCreatesNothing(string claim)
    {
        using var user = await _users.CreateWithClaimsAsync(claim);
        using var admin = await _users.CreateAdminAsync();
        var name = ProductApi.UniqueName();

        await (await user.CreateAsync(ProductApi.NewProduct(name))).ShouldBeProblemAsync(HttpStatusCode.Forbidden);

        (await (await admin.ListAsync()).Content.ReadAsStringAsync()).ShouldNotContain(name);
    }

    [Theory]
    [InlineData("Product.Update")]
    [InlineData("Product.Write")]
    [InlineData("Product.Admin")]
    [InlineData("Admin")]
    public async Task Update_WithAcceptedClaim_Returns204(string claim)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync(ProductApi.NewProduct(amount: 10m));
        using var user = await _users.CreateWithClaimsAsync(claim);

        (await user.UpdateAsync(id, ProductApi.NewProduct(amount: 11m))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await admin.GetOkAsync(id)).Amount.ShouldBe(11m);
    }

    [Theory]
    [InlineData("Product.Read")]
    [InlineData("Product.Create")]
    public async Task Update_WithRefusedClaim_Returns403_AndChangesNothing(string claim)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync(ProductApi.NewProduct(amount: 10m));
        using var user = await _users.CreateWithClaimsAsync(claim);

        await (await user.UpdateAsync(id, ProductApi.NewProduct(amount: 11m))).ShouldBeProblemAsync(HttpStatusCode.Forbidden);

        (await admin.GetOkAsync(id)).Amount.ShouldBe(10m);
    }

    [Theory]
    [InlineData("Product.Delete")]
    [InlineData("Product.Write")]
    [InlineData("Product.Admin")]
    [InlineData("Admin")]
    public async Task Delete_WithAcceptedClaim_Returns204(string claim)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        using var user = await _users.CreateWithClaimsAsync(claim);

        (await user.DeleteAsync(id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await admin.GetAsync(id)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("Product.Read")]
    [InlineData("Product.Create")]
    public async Task Delete_WithRefusedClaim_Returns403_AndDeletesNothing(string claim)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        using var user = await _users.CreateWithClaimsAsync(claim);

        await (await user.DeleteAsync(id)).ShouldBeProblemAsync(HttpStatusCode.Forbidden);

        (await admin.GetAsync(id)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
