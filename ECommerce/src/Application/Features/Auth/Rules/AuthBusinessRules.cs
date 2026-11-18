using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;
using Uludag.Core.Exceptions;
using Uludag.Core.Security.Hashing;

namespace ECommerce.Application.Features.Auth.Rules
{
    // Kimlik doğrulama (Register / Login) ile ilgili tüm iş kuralları burada yazılır.
    public class AuthBusinessRules : BaseBusinessRules
    {
        // Login'de kullanıcı bulunamadığında doğrulanacak sahte hash/salt.
        // Gerçek kullanıcılarla aynı yöntemle (HashingHelper) üretilir; böylece salt/hash boyutları
        // ve dolayısıyla doğrulama maliyeti birebir aynı olur. static => uygulama ömründe bir kez üretilir.
        private static readonly byte[] FakePasswordHash;
        private static readonly byte[] FakePasswordSalt;

        static AuthBusinessRules()
        {
            HashingHelper.CreatePasswordHash(Guid.NewGuid().ToString(), out FakePasswordHash, out FakePasswordSalt);
        }

        private readonly IUserRepository _userRepository;

        public AuthBusinessRules(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // Aynı e-posta ile ikinci bir kullanıcı kaydedilemez => DB'ye bakmak gerekir, yani iş kuralı.
        public async Task EmailCannotExist(string email)
        {
            Domain.Entities.User? userWithSameEmail = await _userRepository.GetByEmailAsync(email);
            if (userWithSameEmail != null)
            {
                throw new BusinessException(AuthConstants.EmailAlreadyExists);
            }
        }

        // [NotNull] => metot hata fırlatmadan dönerse "user artık null değil" bilgisini derleyiciye verir.
        // Kullanıcı yoksa 404 değil 400 döneriz: login'de e-postanın var olup olmadığı belli edilmez.
        public Task UserShouldExistWhenLoggingIn([NotNull] Domain.Entities.User? user, string password)
        {
            if (user is null)
            {
                // Timing attack önlemi: kullanıcı yoksa da sahte bir hash doğrulaması yapılır.
                // Aksi halde "e-posta yok" cevabı hash hesaplanmadığı için belirgin şekilde daha hızlı döner
                // ve yanıt süresine (ms) bakılarak e-postanın kayıtlı olup olmadığı anlaşılabilir.
                // Sonuç önemsizdir; amaç gerçek kullanıcıdakiyle aynı HMACSHA512 maliyetini ödemektir.
                HashingHelper.VerifyPasswordHash(password, FakePasswordHash, FakePasswordSalt);

                throw new BusinessException(AuthConstants.InvalidCredentials);
            }

            return Task.CompletedTask;
        }

        // Girilen şifre, kullanıcının kendi salt'ı ile tekrar hash'lenir ve DB'deki hash ile karşılaştırılır.
        public Task PasswordShouldMatch(Domain.Entities.User user, string password)
        {
            if (!HashingHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                throw new BusinessException(AuthConstants.InvalidCredentials);

            return Task.CompletedTask;
        }
    }
}
