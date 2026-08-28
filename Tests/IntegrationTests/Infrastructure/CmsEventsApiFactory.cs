using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CmsEvents.IntegrationTests.Infrastructure;

public sealed class CmsEventsApiFactory(string testConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PostgreSQL", testConnectionString);
        builder.UseSetting("Auth:User:Username", TestCredentials.ReaderUsername);
        builder.UseSetting("Auth:User:Password", TestCredentials.ReaderPassword);
        builder.UseSetting("Auth:Admin:Username", TestCredentials.AdminUsername);
        builder.UseSetting("Auth:Admin:Password", TestCredentials.AdminPassword);
        builder.UseSetting("Auth:Cms:Username", TestCredentials.CmsUsername);
        builder.UseSetting("Auth:Cms:Password", TestCredentials.CmsPassword);
    }
}