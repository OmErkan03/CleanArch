namespace ECommerce.Application.Features.Auth.Constants
{
    public static class AuthConstants
    {
        public const string EmailAlreadyExists = "Email already exists.";

        // E-posta yanlış da olsa şifre yanlış da olsa aynı mesaj döner;
        // böylece dışarıdan "bu e-posta sistemde kayıtlı mı?" bilgisi sızdırılmaz.
        public const string InvalidCredentials = "Email or password is incorrect.";
    }
}
