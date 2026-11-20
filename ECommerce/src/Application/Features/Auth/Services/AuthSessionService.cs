using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Exceptions;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.RefreshTokens;
using Uludag.Core.Security.Sessions;

namespace ECommerce.Application.Features.Auth.Services
{
    // Kimliği doğrulanmış kullanıcının oturum yaşam döngüsü: giriş, token yenileme, çıkış.
    //
    //   Access token (JWT) : kısa ömürlü (60 dk). Redis'teki bir OTURUMA işaret eder.
    //   Refresh token      : uzun ömürlü (login'den itibaren 3 gün, MUTLAK). Yalnızca yeni access token almaya yarar.
    //                        Tek kullanımlıktır: her yenilemede yenisi verilir (rotation).
    //
    // Yenileme sırasında yetkiler veritabanından yeniden okunur; böylece kullanıcının güncel yetkileri yeni token'a yansır.
    public class AuthSessionService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionStore _sessionStore;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ITokenHelper _tokenHelper;
        private readonly TokenOptions _tokenOptions;

        public AuthSessionService(
            IUserRepository userRepository,
            ISessionStore sessionStore,
            IRefreshTokenStore refreshTokenStore,
            ITokenHelper tokenHelper,
            TokenOptions tokenOptions)
        {
            _userRepository = userRepository;
            _sessionStore = sessionStore;
            _refreshTokenStore = refreshTokenStore;
            _tokenHelper = tokenHelper;
            _tokenOptions = tokenOptions;
        }

        // Şifre + MFA doğrulandıktan sonra çağrılır: yeni bir refresh token ailesi ve ilk access token.
        public async Task<AuthTokens> SignInAsync(Guid userId, string email, CancellationToken cancellationToken = default)
        {
            var refreshExpiresAt = DateTime.UtcNow.AddDays(_tokenOptions.RefreshTokenExpirationDays);

            // Aile önce oturumsuz açılamaz (oturum aile Id'sini, aile oturum anahtarını tutar);
            // bu yüzden önce oturum, sonra aile oluşturulur ve oturum aileye bağlanır.
            var (accessToken, sessionKey) = await CreateSessionAsync(userId, email, refreshTokenFamilyId: null, refreshExpiresAt, cancellationToken);
            var refreshToken = await _refreshTokenStore.CreateAsync(userId, sessionKey, refreshExpiresAt, cancellationToken);

            await _sessionStore.UpdateSessionAsync(sessionKey, s => s.RefreshTokenFamilyId = refreshToken.FamilyId, cancellationToken);

            return new AuthTokens(accessToken, refreshToken.Token, refreshToken.ExpiresAt);
        }

        // Refresh token ile yeni access token + yeni refresh token. Geçersizse UnauthorizedException (401).
        public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var rotation = await _refreshTokenStore.RotateAsync(refreshToken, cancellationToken);

            switch (rotation.Status)
            {
                case RefreshTokenRotationStatus.NotFound:
                    throw new UnauthorizedException(AuthConstants.RefreshTokenInvalid);

                case RefreshTokenRotationStatus.Reused:
                    // Kullanılmış bir token tekrar geldi: ya saldırgan çaldığı token'ı kullanıyor ya da asıl kullanıcı.
                    // Hangisi olduğunu bilemeyiz => aile (Core'da) iptal edildi, ailenin açık oturumu da kapatılır.
                    // Asıl kullanıcı yeniden giriş yapar; saldırganın elindeki token'lar işe yaramaz.
                    if (rotation.PreviousSessionKey is not null)
                        await _sessionStore.RemoveAsync(rotation.PreviousSessionKey, cancellationToken);
                    throw new UnauthorizedException(AuthConstants.RefreshTokenReused);
            }

            // Kullanıcı bu arada silinmiş olabilir; e-posta da değişmiş olabilir => veritabanından okunur.
            var user = await _userRepository.GetByIdAsync(rotation.UserId, cancellationToken);
            if (user is null)
            {
                await _refreshTokenStore.RevokeAsync(rotation.FamilyId!, cancellationToken);
                throw new UnauthorizedException(AuthConstants.RefreshTokenInvalid);
            }

            // Eski access token'ın oturumu kapatılır: yenilemeden sonra yalnızca yeni token geçerli olsun.
            if (rotation.PreviousSessionKey is not null)
                await _sessionStore.RemoveAsync(rotation.PreviousSessionKey, cancellationToken);

            var (accessToken, sessionKey) = await CreateSessionAsync(user.Id, user.Email, rotation.FamilyId, rotation.ExpiresAt, cancellationToken);
            await _refreshTokenStore.SetSessionKeyAsync(rotation.FamilyId!, sessionKey, cancellationToken);

            return new AuthTokens(accessToken, rotation.NewToken!, rotation.ExpiresAt);
        }

        // Bu cihazdan çıkış: oturum ve ona bağlı refresh token ailesi iptal edilir.
        public async Task SignOutAsync(string sessionKey, CancellationToken cancellationToken = default)
        {
            var session = await _sessionStore.GetAsync(sessionKey, cancellationToken);
            await _sessionStore.RemoveAsync(sessionKey, cancellationToken);

            if (session?.RefreshTokenFamilyId is not null)
                await _refreshTokenStore.RevokeAsync(session.RefreshTokenFamilyId, cancellationToken);
        }

        // Tüm cihazlardan çıkış: kullanıcının tüm oturumları ve refresh token aileleri. Kapatılan oturum sayısını döndürür.
        public async Task<int> SignOutEverywhereAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Önce refresh token'lar: aksi halde arada yapılan bir yenileme yeni oturum açabilirdi.
            await _refreshTokenStore.RevokeUserFamiliesAsync(userId, cancellationToken);
            return await _sessionStore.RemoveUserSessionsAsync(userId, cancellationToken);
        }

        private async Task<(AccessToken AccessToken, string SessionKey)> CreateSessionAsync(
            Guid userId, string email, string? refreshTokenFamilyId, DateTime refreshExpiresAt, CancellationToken cancellationToken)
        {
            // Kullanıcının yetkileri Redis'teki oturuma yazılır; AuthorizationBehavior her istekte DB'ye gitmeden bunları kontrol eder.
            var operationClaims = await _userRepository.GetOperationClaimNamesAsync(userId, cancellationToken);

            var now = DateTime.UtcNow;
            // Access token, refresh token ailesinden uzun yaşamaz: 3 günün sonunda gerçekten yeniden giriş gerekir.
            var expiration = Min(now.AddMinutes(_tokenOptions.AccessTokenExpirationMinutes), refreshExpiresAt);

            var sessionKey = await _sessionStore.CreateAsync(new UserSession
            {
                UserId = userId,
                Email = email,
                OperationClaims = operationClaims,
                RefreshTokenFamilyId = refreshTokenFamilyId,
                CreatedAt = now,
                ExpiresAt = expiration
            }, cancellationToken);

            // Token yalnızca oturum anahtarını taşır; kullanıcı bilgileri her istekte Redis'ten okunur.
            return (_tokenHelper.CreateToken(sessionKey, expiration), sessionKey);
        }

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    }

    // RefreshToken istemciye yanıt gövdesinde değil HttpOnly cookie ile verilir (bkz. WebAPI => AuthController).
    public sealed record AuthTokens(AccessToken AccessToken, string RefreshToken, DateTime RefreshTokenExpiration);
}
