namespace Uludag.Core.Security.Sessions
{
    // Oturumları saklayan depo. Login oturumu oluşturur, her istekte JWT'deki anahtarla oturum okunur.
    public interface ISessionStore
    {
        // Oturumu kaydeder ve JWT'ye yazılacak anahtarı döndürür.
        Task<string> CreateAsync(UserSession session, CancellationToken cancellationToken = default);

        // Anahtar yoksa / süresi dolmuşsa / silinmişse null döner.
        Task<UserSession?> GetAsync(string sessionKey, CancellationToken cancellationToken = default);

        // Oturumu sonlandırır: token'ın süresi dolmamış olsa bile artık geçersizdir (logout, yetki değişikliği...).
        Task RemoveAsync(string sessionKey, CancellationToken cancellationToken = default);

        // Kullanıcının AÇIK OLAN TÜM oturumlarını (farklı cihaz/tarayıcı) günceller; kalan süreleri değişmez.
        // Oturum her istekte Redis'ten okunduğu için değişiklik kullanıcının bir sonraki isteğinde geçerli olur,
        // yeniden giriş yapması gerekmez. Güncellenen oturum sayısını döndürür.
        Task<int> UpdateUserSessionsAsync(Guid userId, Action<UserSession> update, CancellationToken cancellationToken = default);
    }
}
