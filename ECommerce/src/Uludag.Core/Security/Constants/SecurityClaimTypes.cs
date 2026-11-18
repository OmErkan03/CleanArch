namespace Uludag.Core.Security.Constants
{
    // Token'a yazılan / token'dan okunan claim tiplerinin adları.
    // JwtHelper (yazan), CurrentUser (okuyan) ve Program.cs (RoleClaimType) aynı sabiti kullanır.
    public static class SecurityClaimTypes
    {
        // Kullanıcının her operation claim'i ayrı bir "role" claim'i olarak token'a eklenir.
        // Birden fazlaysa JWT payload'ında dizi olarak görünür: "role": ["Product.Read", "Order.Write"]
        public const string OperationClaim = "role";
    }
}
