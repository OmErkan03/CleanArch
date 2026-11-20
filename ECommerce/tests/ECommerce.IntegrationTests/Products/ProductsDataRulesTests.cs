using System.Net;
using System.Net.Http.Json;
using System.Text;
using ECommerce.IntegrationTests.Infrastructure;

namespace ECommerce.IntegrationTests.Products;

// AC-7 — ürün veri kuralları, docs/domain.md'den (BR-9, BR-19…BR-23): ihlal → 400 ProblemDetails,
// hiçbir şey saklanmaz; sınırın geçerli tarafı kabul edilir. Beklentiler kurallardan yazılır, koddan değil.
public class ProductsDataRulesTests(ApiFactory factory)
{
    private readonly TestUsers _users = new(factory);

    // Each invalid body carries a marker that only this request uses — a unique name, or (when the
    // name itself is the invalid part) a unique stock — so "nothing stored" is checked without
    // counting a table other parallel tests also write to.
    //
    // Expectations the code does NOT meet today are not committed as red tests (spec 0002, AC-11);
    // each is a bug entry in specs/backlog.md with its request and observed response, and its row
    // returns here in the fix workflow:
    //   BR-19 negative amount (-0.01)        → B-8  (500)
    //   BR-20 empty / whitespace name        → B-9  (201 / 204)
    //   BR-20 name of 101 chars              → B-10 (500)
    //   BR-21 negative stock (-1)            → B-11 (201 / 204)
    //   BR-22 "", "12$" / "TRYX" currency     → B-12 (201 / 204 / 500)
    //   BR-23 three decimals (1.005)         → B-13 (201 / 204)
    //   BR-9 update to another product's name → B-7 (204)
    private static object Body(string @case, string name, int marker) => @case switch
    {
        "BR-20 null name" => new { name = (string?)null, amount = 1m, currency = "TRY", stock = marker },
        _ => throw new ArgumentOutOfRangeException(nameof(@case), @case, null)
    };

    public static TheoryData<string> InvalidCases() => new("BR-20 null name");

    private static int NewMarker() => Random.Shared.Next(1_000_000, int.MaxValue);

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public async Task Create_ViolatingRule_Returns400_AndStoresNothing(string @case)
    {
        using var admin = await _users.CreateAdminAsync();
        var name = ProductApi.UniqueName();
        var marker = NewMarker();

        await (await admin.CreateAsync(Body(@case, name, marker))).ShouldBeProblemAsync(HttpStatusCode.BadRequest);

        var list = await (await admin.ListAsync()).Content.ReadFromJsonAsync<List<ProductDto>>();
        list!.ShouldNotContain(p => p.Name == name || p.Stock == marker, @case);
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public async Task Update_ViolatingRule_Returns400_AndChangesNothing(string @case)
    {
        using var admin = await _users.CreateAdminAsync();
        var id = await admin.CreateOkAsync();
        var before = await admin.GetOkAsync(id);

        await (await admin.UpdateAsync(id, Body(@case, ProductApi.UniqueName(), NewMarker()))).ShouldBeProblemAsync(HttpStatusCode.BadRequest);

        (await admin.GetOkAsync(id)).ShouldBe(before, @case);
    }

    // BR-9 on create
    [Fact]
    public async Task Create_DuplicateName_Returns400_AndStoresNothing()
    {
        using var admin = await _users.CreateAdminAsync();
        var name = ProductApi.UniqueName();
        await admin.CreateOkAsync(ProductApi.NewProduct(name));

        await (await admin.CreateAsync(ProductApi.NewProduct(name))).ShouldBeProblemAsync(HttpStatusCode.BadRequest);

        var list = await (await admin.ListAsync()).Content.ReadFromJsonAsync<List<ProductDto>>();
        list!.Count(p => p.Name == name).ShouldBe(1);
    }

    // BR-9: keeping one's own name is not a duplicate.
    [Fact]
    public async Task Update_KeepingOwnName_Returns204()
    {
        using var admin = await _users.CreateAdminAsync();
        var name = ProductApi.UniqueName();
        var id = await admin.CreateOkAsync(ProductApi.NewProduct(name, stock: 1));

        (await admin.UpdateAsync(id, ProductApi.NewProduct(name, stock: 2))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await admin.GetOkAsync(id)).Stock.ShouldBe(2);
    }

    // Valid side of the boundaries: zero amount (BR-19), zero stock (BR-21), 100-char name (BR-20), 2 decimals (BR-23).
    [Fact]
    public async Task Create_AtValidBoundaries_Returns201_AndPersistsExactly()
    {
        using var admin = await _users.CreateAdminAsync();
        var name = (ProductApi.UniqueName() + new string('b', 100))[..100];

        var id = await admin.CreateOkAsync(new { name, amount = 0.00m, currency = "EUR", stock = 0 });
        var free = await admin.CreateOkAsync(new { name = ProductApi.UniqueName(), amount = 99.99m, currency = "USD", stock = 0 });

        (await admin.GetOkAsync(id)).ShouldBe(new ProductDto(id, name, 0m, "EUR", 0));
        (await admin.GetOkAsync(free)).Amount.ShouldBe(99.99m);
    }

    // Malformed input is a client error, not a server error.
    [Theory]
    [InlineData("{ not json")]
    [InlineData("{\"name\":\"x\",\"amount\":\"abc\",\"currency\":\"TRY\",\"stock\":1}")]
    [InlineData("{\"name\":\"x\",\"amount\":1,\"currency\":\"TRY\",\"stock\":1.5}")]
    public async Task Create_MalformedJson_Returns400(string json)
    {
        using var admin = await _users.CreateAdminAsync();
        var response = await admin.PostAsync(ProductApi.Route, new StringContent(json, Encoding.UTF8, "application/json"));
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
    }
}
