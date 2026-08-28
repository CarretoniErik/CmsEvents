using CmsEvents.Domain.Entities;
using CmsEvents.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CmsEvents.Infrastructure.Persistence.DbContext;

public class ReadDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<CmsEntity> CmsEntities => Set<CmsEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new CmsEntityConfiguration());
}
