namespace Uludag.Core.Security.JWT
{
    // Login sonrası istemciye dönen değer.
    // İstemci sonraki isteklerde "Authorization: Bearer {Token}" başlığını gönderir.
    public class AccessToken
    {
        public string Token { get; set; } = string.Empty;

        // UTC. İstemci bu tarihten önce yeniden login olmalıdır.
        public DateTime Expiration { get; set; }
    }
}
