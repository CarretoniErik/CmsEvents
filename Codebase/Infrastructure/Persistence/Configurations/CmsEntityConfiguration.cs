using CmsEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CmsEvents.Infrastructure.Persistence.Configurations;

public sealed class CmsEntityConfiguration : IEntityTypeConfiguration<CmsEntity>
{
    public void Configure(EntityTypeBuilder<CmsEntity> builder)
    {
        builder.ToTable("cms_entities");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("id")
               .HasMaxLength(256)
               .IsRequired();

        builder.Property(e => e.Version)
               .HasColumnName("version")
               .IsRequired();

        // Store the open payload as jsonb (queryable/indexable in Postgres)
        builder.Property(e => e.Payload)
               .HasColumnName("payload")
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(e => e.CmsTimestamp)
               .HasColumnName("cms_timestamp")
               .IsRequired();

        builder.Property(e => e.IsUnpublishedByCms)
               .HasColumnName("is_unpublished_by_cms")
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(e => e.IsDisabledByAdmin)
               .HasColumnName("is_disabled_by_admin")
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired();

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired();

        // Computed property — not persisted
        builder.Ignore(e => e.IsVisibleToUsers);

        // Index to speed up the common "visible to users" read query
        builder.HasIndex(e => new { e.IsUnpublishedByCms, e.IsDisabledByAdmin })
               .HasDatabaseName("ix_cms_entities_visibility");
    }
}