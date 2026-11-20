using Uludag.Core.Security.Constants;

namespace Uludag.Core.Security.Ownership
{
    // IDataOwnershipContext'in ICurrentUser (oturumdaki claim'ler) üzerinden çalışan implementasyonu.
    public class DataOwnershipContext(ICurrentUser currentUser) : IDataOwnershipContext
    {
        public Guid? UserId => currentUser.IsAuthenticated ? currentUser.UserId : null;

        public bool CanAccessAllOf(string resource)
        {
            if (!currentUser.IsAuthenticated)
                return false;

            var claims = currentUser.OperationClaims;
            return claims.Contains(GeneralOperationClaims.Admin, StringComparer.Ordinal)
                || claims.Contains($"{resource}.Admin", StringComparer.Ordinal);
        }
    }
}
