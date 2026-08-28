using Microsoft.Extensions.Options;

namespace CmsEvents.Api.Authentication.Credentials;

public sealed class CredentialsValidator(IOptions<AuthOptions> options) : ICredentialsValidator
{
    private readonly AuthOptions _options = options.Value;

    public bool TryValidate(string username, string password, out AuthenticatedUser user)
    {
        if (Matches(_options.Cms, username, password))
        {
            user = new(username, UserRole.Cms);
            return true;
        }

        if (Matches(_options.Admin, username, password))
        {
            user = new(username, UserRole.Admin);
            return true;
        }

        if (Matches(_options.User, username, password))
        {
            user = new(username, UserRole.User);
            return true;
        }

        user = null!;
        return false;
    }

    private static bool Matches(CredentialOptions credentials, string username, string password) => credentials.Username == username && credentials.Password == password;
}