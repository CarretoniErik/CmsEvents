using CmsEvents.Api.Authentication;
using CmsEvents.Api.Authentication.Credentials;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;

namespace CmsEvents.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddBasicAuthentication(configuration)
            .AddAuthorizationBuilder()
            .AddPolicy("Cms", policy =>
            {
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(UserRole.Cms.ToString());
            })
            .AddPolicy("Consumer", policy =>
            {
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(UserRole.User.ToString(), UserRole.Admin.ToString());
            })
            .AddPolicy("Admin", policy =>
            {
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(UserRole.Admin.ToString());
            });

        services.AddOpenApiDocumentation();
        return services;
    }

    private static IServiceCollection AddBasicAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AuthOptions>()
            .Bind(configuration.GetSection("Auth"))
            .ValidateOnStart();

        services.AddScoped<ICredentialsValidator, CredentialsValidator>();
        services.AddAuthentication("Basic").AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
        return services;
    }

    private static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                var basicScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic authentication"
                };
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Basic"] = basicScheme
                };
                foreach (var path in document.Paths.Values)
                {
                    if (path.Operations is null) continue;
                    foreach (var operation in path.Operations.Values)
                    {
                        operation.Security ??= [];
                        operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Basic", document)] = [] });
                    }
                }
                return Task.CompletedTask;
            });
        });

        return services;
    }
}
