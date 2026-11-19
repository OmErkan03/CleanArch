namespace ECommerce.Application.Features.Auth.Constants
{
    public static class AuthConstants
    {
        public const string EmailAlreadyExists = "Email already exists.";

        // E-posta yanlış da olsa şifre yanlış da olsa aynı mesaj döner;
        // böylece dışarıdan "bu e-posta sistemde kayıtlı mı?" bilgisi sızdırılmaz.
        public const string InvalidCredentials = "Email or password is incorrect.";

        // ---------- MFA ----------
        public const string MfaSetupRequired =
            "Multi-factor authentication is not set up. Call /api/auth/mfa/setup with the MFA token, scan the QR code and confirm with a code.";
        public const string MfaVerificationRequired =
            "Enter the code from your authenticator app at /api/auth/mfa/verify with the MFA token.";

        // Token yok / süresi doldu / kullanıldı / başka bir adım için verildi => hepsine aynı cevap (ayrıntı sızdırılmaz).
        public const string MfaTokenInvalid = "MFA token is invalid or expired. Please log in again.";
        public const string MfaSetupNotStarted = "MFA setup has not been started. Call /api/auth/mfa/setup first.";
        public const string MfaCodeInvalid = "The verification code is invalid.";
        public const string MfaTooManyAttempts = "Too many invalid codes. Please log in again.";
    }
}
