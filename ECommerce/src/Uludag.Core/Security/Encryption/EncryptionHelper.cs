using System.Security.Cryptography;
using System.Text;

namespace Uludag.Core.Security.Encryption
{
    // HASHING vs ENCRYPTION
    // ---------------------
    // HashingHelper  : Tek yönlüdür. Hash'ten orijinal veriye geri dönülemez. (Şifre saklama)
    // EncryptionHelper: Çift yönlüdür. Doğru anahtarla şifreli veri tekrar çözülebilir. (Hassas veri saklama/taşıma)
    //
    // SİMETRİK (AES-GCM)   : Şifreleme ve çözme için AYNI anahtar kullanılır. Hızlıdır, büyük veride uygundur.
    //                        Sorun: anahtarın karşı tarafa güvenli şekilde iletilmesi.
    // ASİMETRİK (RSA-OAEP) : Public key ile şifrelenir, sadece Private key ile çözülür. Anahtar paylaşımı kolaydır,
    //                        ama yavaştır ve anahtar boyutuyla sınırlı (2048 bit için ~190 byte) veri şifreleyebilir.
    // HİBRİT               : İkisinin birleşimi (HTTPS/TLS de böyle çalışır). Veri rastgele bir AES anahtarıyla şifrelenir,
    //                        o AES anahtarı da RSA public key ile şifrelenir.
    public static class EncryptionHelper
    {
        private const int AesKeySize = 32;   // 256 bit
        private const int NonceSize = 12;    // AES-GCM için önerilen nonce (IV) boyutu
        private const int TagSize = 16;      // 128 bit doğrulama etiketi (verinin değiştirilmediğini garanti eder)
        private static readonly RSAEncryptionPadding RsaPadding = RSAEncryptionPadding.OaepSHA256;

        #region Simetrik (AES-GCM)

        // Rastgele 256 bitlik AES anahtarı üretir. Base64 olarak config/secret store'da saklanabilir.
        public static byte[] CreateSymmetricKey() => RandomNumberGenerator.GetBytes(AesKeySize);

        public static string CreateSymmetricKeyBase64() => Convert.ToBase64String(CreateSymmetricKey());

        public static byte[] EncryptSymmetric(byte[] plainBytes, byte[] key)
        {
            ValidateSymmetricKey(key);

            // HashingHelper'daki salt gibi: her şifrelemede yeni rastgele nonce => aynı veri her seferinde farklı çıktı verir.
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Encrypt(nonce, plainBytes, cipher, tag);
            }

            // Çıktı formatı: [nonce | tag | cipher] — çözerken nonce ve tag'e ihtiyaç var, bu yüzden birlikte saklanır.
            var result = new byte[NonceSize + TagSize + cipher.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(cipher, 0, result, NonceSize + TagSize, cipher.Length);
            return result;
        }

        public static byte[] DecryptSymmetric(byte[] encryptedBytes, byte[] key)
        {
            ValidateSymmetricKey(key);
            if (encryptedBytes.Length < NonceSize + TagSize)
                throw new CryptographicException("Şifreli veri geçersiz.");

            var nonce = encryptedBytes.AsSpan(0, NonceSize);
            var tag = encryptedBytes.AsSpan(NonceSize, TagSize);
            var cipher = encryptedBytes.AsSpan(NonceSize + TagSize);
            var plain = new byte[cipher.Length];

            using (var aes = new AesGcm(key, TagSize))
            {
                // Anahtar yanlışsa veya veri değiştirildiyse tag tutmaz ve CryptographicException fırlatılır.
                aes.Decrypt(nonce, cipher, tag, plain);
            }
            return plain;
        }

        public static string EncryptSymmetric(string plainText, string base64Key) =>
            Convert.ToBase64String(EncryptSymmetric(Encoding.UTF8.GetBytes(plainText), Convert.FromBase64String(base64Key)));

        public static string DecryptSymmetric(string cipherText, string base64Key) =>
            Encoding.UTF8.GetString(DecryptSymmetric(Convert.FromBase64String(cipherText), Convert.FromBase64String(base64Key)));

        private static void ValidateSymmetricKey(byte[] key)
        {
            if (key is null || key.Length != AesKeySize)
                throw new ArgumentException($"AES anahtarı {AesKeySize} byte (256 bit) olmalıdır.", nameof(key));
        }

        #endregion

        #region Asimetrik (RSA-OAEP)

        // Public/Private key çifti üretir (PEM formatında).
        // PublicKey herkesle paylaşılabilir; PrivateKey sadece veriyi çözecek tarafta GİZLİ kalmalıdır.
        public static void CreateAsymmetricKeyPair(out string publicKeyPem, out string privateKeyPem, int keySizeInBits = 2048)
        {
            using (var rsa = RSA.Create(keySizeInBits))
            {
                publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
                privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            }
        }

        public static byte[] EncryptAsymmetric(byte[] plainBytes, string publicKeyPem)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicKeyPem);
                return rsa.Encrypt(plainBytes, RsaPadding);
            }
        }

        public static byte[] DecryptAsymmetric(byte[] encryptedBytes, string privateKeyPem)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateKeyPem);
                return rsa.Decrypt(encryptedBytes, RsaPadding);
            }
        }

        public static string EncryptAsymmetric(string plainText, string publicKeyPem) =>
            Convert.ToBase64String(EncryptAsymmetric(Encoding.UTF8.GetBytes(plainText), publicKeyPem));

        public static string DecryptAsymmetric(string cipherText, string privateKeyPem) =>
            Encoding.UTF8.GetString(DecryptAsymmetric(Convert.FromBase64String(cipherText), privateKeyPem));

        #endregion

        #region Hibrit (RSA + AES)

        // RSA'nın boyut sınırına takılmadan büyük veriyi asimetrik olarak güvenle iletmek için:
        // 1) Rastgele bir AES anahtarı üret, veriyi AES ile şifrele (hızlı, boyut sınırı yok)
        // 2) AES anahtarını alıcının RSA public key'i ile şifrele
        // Çıktı formatı: [şifreli AES anahtarı uzunluğu (4 byte) | şifreli AES anahtarı | AES çıktısı]
        public static byte[] EncryptHybrid(byte[] plainBytes, string publicKeyPem)
        {
            var aesKey = CreateSymmetricKey();
            try
            {
                var encryptedKey = EncryptAsymmetric(aesKey, publicKeyPem);
                var encryptedData = EncryptSymmetric(plainBytes, aesKey);

                var result = new byte[sizeof(int) + encryptedKey.Length + encryptedData.Length];
                BitConverter.TryWriteBytes(result.AsSpan(0, sizeof(int)), encryptedKey.Length);
                Buffer.BlockCopy(encryptedKey, 0, result, sizeof(int), encryptedKey.Length);
                Buffer.BlockCopy(encryptedData, 0, result, sizeof(int) + encryptedKey.Length, encryptedData.Length);
                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(aesKey); // Anahtarı bellekte bırakma
            }
        }

        public static byte[] DecryptHybrid(byte[] encryptedBytes, string privateKeyPem)
        {
            if (encryptedBytes.Length < sizeof(int))
                throw new CryptographicException("Şifreli veri geçersiz.");

            var keyLength = BitConverter.ToInt32(encryptedBytes, 0);
            if (keyLength <= 0 || keyLength > encryptedBytes.Length - sizeof(int))
                throw new CryptographicException("Şifreli veri geçersiz.");

            var encryptedKey = encryptedBytes.AsSpan(sizeof(int), keyLength).ToArray();
            var encryptedData = encryptedBytes.AsSpan(sizeof(int) + keyLength).ToArray();

            var aesKey = DecryptAsymmetric(encryptedKey, privateKeyPem);
            try
            {
                return DecryptSymmetric(encryptedData, aesKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(aesKey);
            }
        }

        public static string EncryptHybrid(string plainText, string publicKeyPem) =>
            Convert.ToBase64String(EncryptHybrid(Encoding.UTF8.GetBytes(plainText), publicKeyPem));

        public static string DecryptHybrid(string cipherText, string privateKeyPem) =>
            Encoding.UTF8.GetString(DecryptHybrid(Convert.FromBase64String(cipherText), privateKeyPem));

        #endregion
    }
}
