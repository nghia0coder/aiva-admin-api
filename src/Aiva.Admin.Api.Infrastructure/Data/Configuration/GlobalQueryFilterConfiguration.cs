namespace Aiva.Admin.Api.Infrastructure.Data.Configuration;

using System.Linq.Expressions;
using Core.Commons.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// Helper class for configuring global query filters
/// </summary>
public static class GlobalQueryFilterConfiguration
{
  /// <summary>
  /// Apply soft delete query filter to all entities that implement ISoftDeletable
  /// This is an alternative approach using interface-based detection
  /// </summary>
  public static void ApplySoftDeleteFilter(this ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
      {
        ApplySoftDeleteFilterToEntity(modelBuilder, entityType);
      }
    }
  }

  private static void ApplySoftDeleteFilterToEntity(ModelBuilder modelBuilder, IMutableEntityType entityType)
  {
    // Create: entity => !entity.IsDeleted
    var parameter = Expression.Parameter(entityType.ClrType, "entity");
    var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
    var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));
    var lambda = Expression.Lambda(notDeleted, parameter);

    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
  }

  /// <summary>
  /// Alternative: Apply filter using a generic method with better type safety
  /// </summary>
  public static void ApplySoftDeleteFilter<TEntity>(this ModelBuilder modelBuilder)
    where TEntity : class, ISoftDeletable
  {
    modelBuilder.Entity<TEntity>()
      .HasQueryFilter(e => !e.IsDeleted);
  }
}
