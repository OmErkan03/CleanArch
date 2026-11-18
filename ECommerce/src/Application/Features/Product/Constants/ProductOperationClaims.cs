namespace ECommerce.Application.Features.Product.Constants
{
    // Product işlemleri için operation claim adları (varsayılan yapı).
    //   Create / Update / Delete => üst yetkisi Write   |   GetById / GetAll => Read   |   Admin => hepsi
    // Her Command/Query, ISecuredRequest.Roles ile hangi claim'leri kabul ettiğini bu sabitlerle belirtir.
    // Yeni bir sabit eklenirse Persistence/Seeds/OperationClaimSeed.cs'e de eklenmeli (sabit Id ile) ve migration alınmalıdır.
    public static class ProductOperationClaims
    {
        private const string Section = "Product";

        public const string Admin = $"{Section}.Admin";

        public const string Read = $"{Section}.Read";
        public const string Write = $"{Section}.Write";

        public const string Create = $"{Section}.Create";
        public const string Update = $"{Section}.Update";
        public const string Delete = $"{Section}.Delete";
    }
}
