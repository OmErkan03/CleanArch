namespace Uludag.Core.Caching
{
    // Uygulama kodu Redis'e doğrudan bağımlı olmasın diye kullanılan soyutlama.
    // Değerler JSON'a çevrilerek saklanır; Redis yerine başka bir cache yazmak gerekirse yalnızca implementasyon değişir.
    public interface ICacheService
    {
        // Anahtar yoksa (hiç yazılmamış ya da süresi dolmuş) null döner.
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        // expiration verilirse anahtar o süre sonunda Redis tarafından otomatik silinir (TTL).
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        // Yalnızca anahtar VARSA değerini değiştirir ve kalan süresini (TTL) KORUR.
        // Anahtar yoksa (süresi dolmuş / silinmiş) hiçbir şey yazmaz ve false döner; böylece silinmiş bir kayıt yanlışlıkla geri gelmez.
        Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken cancellationToken = default);

        // Yalnızca anahtar YOKSA yazar (atomik). Yazdıysa true, anahtar zaten varsa false döner.
        // "Bu iş daha önce yapıldı mı?" kontrolünü yarış durumu (race condition) olmadan yapmak için kullanılır.
        Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        // ---------- Set (tekrarsız string kümesi) ----------
        // Bir kullanıcının oturum anahtarları gibi "bir anahtara bağlı birden çok değer" tutmak için kullanılır.

        Task SetAddAsync(string key, string member, CancellationToken cancellationToken = default);

        Task<string[]> SetMembersAsync(string key, CancellationToken cancellationToken = default);

        Task SetRemoveAsync(string key, params string[] members);

        // Anahtarın kalan süresini yalnızca verilen süreden KISAYSA (ya da hiç süresi yoksa) uzatır; asla kısaltmaz.
        Task ExtendExpirationAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);
    }
}
