using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // IRequest => şimdilik geriye değer döndürmez (token henüz yok).
    // Başarılıysa 200, e-posta/şifre hatalıysa AuthBusinessRules BusinessException fırlatır (400).
    public class LoginCommand : IRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public class LoginCommandHandler : IRequestHandler<LoginCommand>
        {
            private readonly IUserRepository _userRepository;
            private readonly AuthBusinessRules _authBusinessRules;

            public LoginCommandHandler(IUserRepository userRepository, AuthBusinessRules authBusinessRules)
            {
                _userRepository = userRepository;
                _authBusinessRules = authBusinessRules;
            }

            public async Task Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                await _authBusinessRules.UserShouldExistWhenLoggingIn(user, request.Password);

                await _authBusinessRules.PasswordShouldMatch(user, request.Password);
            }
        }
    }
}
