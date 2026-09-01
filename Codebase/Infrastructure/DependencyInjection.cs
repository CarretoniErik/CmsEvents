using CmsEvents.Application.Persistence;
using CmsEvents.Application.Persistence.Abstractions;
using CmsEvents.Infrastructure.Persistence;
using CmsEvents.Infrastructure.Persistence.DbContext;
using CmsEvents.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CmsEvents.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("PostgreSQL connection string is not configured.");

        services.AddDbContext<ReadDbContext>(options => options.UseNpgsql(connectionString));
        services.AddDbContext<WriteDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<WriteDbContext>());
        services.AddScoped<ICmsEntityReadRepository, CmsEntityReadRepository>();
        services.AddScoped<ICmsEntityWriteRepository, CmsEntityWriteRepository>();
        services.AddScoped<IConcurrencyConflictHandler, ConcurrencyConflictHandler>();

        return services;
    }
}