using CmsEvents.Api.Authentication.Credentials;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace CmsEvents.Api.Authentication;

public sealed class BasicAuthenticationHandler
(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ICredentialsValidator credentialsValidator,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var header = authorizationHeader.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var encodedCredentials = header["Basic ".Length..].Trim();
        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic Authentication credentials"));
        }

        var separatorIndex = credentials.IndexOf(':');
        if (separatorIndex < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic Authentication credentials"));
        }

        var username = credentials[..separatorIndex];
        var password = credentials[(separatorIndex + 1)..];

        if (!credentialsValidator.TryValidate(username, password, out var user))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}