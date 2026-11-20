using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OtpNet;

namespace ECommerce.IntegrationTests.Infrastructure;

// Kimliği doğrulanmış test kullanıcıları — GERÇEK akışla: register → login → MFA setup → TOTP onayı → JWT.
// Kısayol yok: token'ı uygulamanın kendisi üretir, oturum Redis'e uygulamanın kendisi yazar.
public sealed class TestUsers(ApiFactory factory)
{
    private const string Password = "Test-Password-123!";

    // Admin claim'i yalnızca ilk kez doğrudan verilir (arrange); bkz. ApiFactory.GrantClaimDirectlyAsync.
    public async Task<HttpClient> CreateAdminAsync()
    {
        var (userId, email) = await RegisterAsync();
        await factory.GrantClaimDirectlyAsync(userId, "Admin");
        return await SignInAsync(email);
    }

    // Claim'ler admin tarafından gerçek API ile verilir; kullanıcı ARDINDAN giriş yapar.
    public async Task<HttpClient> CreateWithClaimsAsync(params string[] claimNames)
    {
        var (userId, email) = await RegisterAsync();
        if (claimNames.Length > 0)
        {
            using var admin = await CreateAdminAsync();
            foreach (var claimName in claimNames)
            {
                var claimId = await factory.ClaimIdAsync(claimName);
                var grant = await admin.PostAsync($"/api/users/{userId}/operation-claims/{claimId}", null);
                grant.IsSuccessStatusCode.ShouldBeTrue($"granting {claimName}: {(int)grant.StatusCode} {await grant.Content.ReadAsStringAsync()}");
            }
        }
        return await SignInAsync(email);
    }

    private async Task<(Guid UserId, string Email)> RegisterAsync()
    {
        using var client = factory.CreateGuardedClient();
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password });
        response.IsSuccessStatusCode.ShouldBeTrue($"register: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<Guid>(), email);
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        using var anonymous = factory.CreateGuardedClient();

        var login = await PostJsonAsync(anonymous, "/api/auth/login", new { email, password = Password });
        login.GetProperty("status").GetString().ShouldBe("MfaSetupRequired");
        var mfaToken = login.GetProperty("mfaToken").GetString()!;

        var setup = await PostJsonAsync(anonymous, "/api/auth/mfa/setup", new { mfaToken });
        var secret = setup.GetProperty("secret").GetString()!;
        var code = new Totp(Base32Encoding.ToBytes(secret), step: 30, totpSize: 6).ComputeTotp();

        var confirmed = await PostJsonAsync(anonymous, "/api/auth/mfa/setup/confirm", new { mfaToken, code });
        confirmed.GetProperty("status").GetString().ShouldBe("Succeeded");
        var token = confirmed.GetProperty("accessToken").GetProperty("token").GetString()!;

        var client = factory.CreateGuardedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> PostJsonAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        var text = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.ShouldBeTrue($"{url}: {(int)response.StatusCode} {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
