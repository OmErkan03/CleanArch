using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }

        // Kullanıcı authenticator kurulumunu tamamladı mı? false ise login JWT yerine "MFA kurulumu gerekli" döner.
        public bool IsMfaEnabled { get; set; }

        // TOTP gizli anahtarı, AES-GCM ile ŞİFRELİ (bkz. Uludag.Core => ITotpHelper.EncryptSecret). Kurulum yapılmadıysa null.
        public string? MfaSecret { get; set; }

        // Kullanıcının sahip olduğu yetkiler (çoka-çok, ara tablo: UserOperationClaims).
        public ICollection<OperationClaim> OperationClaims { get; set; } = [];
    }
}
