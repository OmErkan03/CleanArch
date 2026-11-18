using ECommerce.Application.Features.Auth.Commands.Register;

namespace ECommerce.Application.Features.Auth.Mappers
{
    // Auth ile ilgili tüm dönüşümler (Command => Entity) tek bir yerde yapılır.
    public static class AuthMapper
    {
        // Şifre burada hash'lenmez; hash/salt handler'da HashingHelper ile üretilip buraya verilir.
        // Düz şifre (request.Password) asla entity'ye yazılmaz.
        public static Domain.Entities.User MapRegisterCommandToUser(RegisterCommand command, byte[] passwordHash, byte[] passwordSalt) => new()
        {
            Email = command.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };
    }
}
