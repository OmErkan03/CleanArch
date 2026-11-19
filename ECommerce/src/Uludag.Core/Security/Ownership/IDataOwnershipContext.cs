namespace Uludag.Core.Security.Ownership
{
    // Veri katmanının IDOR kontrolü için ihtiyaç duyduğu bilgi: "kayıtlara kim adına erişiliyor?"
    // DbContext claim adlarını / HttpContext'i bilmez; yalnızca bu arayüzü kullanır.
    public interface IDataOwnershipContext
    {
        // İsteği yapan kullanıcı. Oturum yoksa null => sahipli hiçbir kayda erişilemez (fail-closed).
        Guid? UserId { get; }

        // Kullanıcı verilen kaynağın (ör. "Order") TÜM kayıtlarına, sahibinden bağımsız erişebilir mi?
        // Genel "Admin" veya "{resource}.Admin" claim'i gerekir (bkz. ISecuredRequest'teki claim yapısı).
        bool CanAccessAllOf(string resource);
    }
}
