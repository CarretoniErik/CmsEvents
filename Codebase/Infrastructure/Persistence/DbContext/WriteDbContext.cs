using CmsEvents.Application.Persistence;
using CmsEvents.Domain.Entities;
using CmsEvents.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CmsEvents.Infrastructure.Persistence.DbContext;

public sealed class WriteDbContext(DbContextOptions<WriteDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options), IUnitOfWork
{
    public DbSet<CmsEntity> CmsEntities => Set<CmsEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new CmsEntityConfiguration());
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
