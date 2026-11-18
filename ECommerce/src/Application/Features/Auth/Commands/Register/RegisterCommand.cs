using ECommerce.Application.Features.Auth.Mappers;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Hashing;

namespace ECommerce.Application.Features.Auth.Commands.Register
{
    // IRequest<Guid> => kayıt olan kullanıcının Id'sini döndürür.
    public class RegisterCommand : IRequest<Guid>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
        {
            private readonly IUserRepository _userRepository;
            private readonly AuthBusinessRules authBusinessRules;

            public RegisterCommandHandler(IUserRepository userRepository, AuthBusinessRules authBusinessRules)
            {
                _userRepository = userRepository;
                this.authBusinessRules = authBusinessRules;
            }

            public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
            {
                await authBusinessRules.EmailCannotExist(request.Email);

                // Şifre veritabanına düz metin olarak yazılmaz.
                // HashingHelper rastgele bir salt üretir ve şifreyi bu salt ile HMACSHA512 kullanarak hash'ler.
                HashingHelper.CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

                var user = AuthMapper.MapRegisterCommandToUser(request, passwordHash, passwordSalt);

                await _userRepository.AddAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                return user.Id;
            }
        }
    }
}
