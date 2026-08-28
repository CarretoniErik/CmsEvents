using CmsEvents.Api.Authentication.Credentials;

namespace CmsEvents.Api.Authentication;

public sealed class AuthOptions
{
    public CredentialOptions Cms { get; init; } = new();
    public CredentialOptions User { get; init; } = new();
    public CredentialOptions Admin { get; init; } = new();
}