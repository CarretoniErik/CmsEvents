using CmsEvents.Infrastructure.Persistence.DbContext;
using Microsoft.Extensions.DependencyInjection;

namespace CmsEvents.Infrastructure;

public static class DatabaseMigration
{
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        var response = await db.Database.EnsureCreatedAsync();
        Console.WriteLine(response);
    }
}