namespace Uludag.Core.Mediation;

/// <summary>
/// Yetki (OperationClaim) gerektiren istekleri işaretler.
/// <see cref="Behaviors.AuthorizationBehavior{TRequest, TResponse}"/> kullanıcının token'ındaki operation claim'leri
/// <see cref="Roles"/> listesiyle METİNSEL olarak karşılaştırır; listedeki claim'lerden EN AZ BİRİNE sahip olmak yeterlidir.
/// <para>
/// Varsayılan yapı (her entity için):
/// <list type="bullet">
/// <item><c>Entity.Create</c>, <c>Entity.Update</c>, <c>Entity.Delete</c> => üst yetkisi <c>Entity.Write</c></item>
/// <item><c>Entity.Read</c> => sorgular (GetById, GetAll)</item>
/// <item><c>Entity.Admin</c> => o entity üzerindeki her işlem</item>
/// </list>
/// Örnek: <c>CreateProductCommand.Roles => ["Product.Admin", "Product.Write", "Product.Create"]</c>
/// </para>
/// </summary>
public interface ISecuredRequest
{
    string[] Roles { get; }
}
