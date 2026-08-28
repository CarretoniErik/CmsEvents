namespace CmsEvents.Api.Authentication.Credentials;

public interface ICredentialsValidator
{
    bool TryValidate(string username, string password, out AuthenticatedUser user);
}