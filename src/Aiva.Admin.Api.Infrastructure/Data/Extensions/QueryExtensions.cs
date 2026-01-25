namespace Aiva.Admin.Api.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for querying entities with soft delete support
/// </summary>
public static class QueryExtensions
{
  /// <summary>
  /// Include soft-deleted entities in query results.
  /// Bypasses global query filter for IsDeleted property.
  /// Use case: Admin panels, audit logs, restore functionality
  /// </summary>
  /// <example>
  /// var allConversations = await _dbContext.Conversations
  ///   .IncludeDeleted()
  ///   .ToListAsync();
  /// </example>
  public static IQueryable<TEntity> IncludeDeleted<TEntity>(this IQueryable<TEntity> query)
    where TEntity : class
  {
    return query.IgnoreQueryFilters();
  }

  /// <summary>
  /// Only get soft-deleted entities.
  /// </summary>
  /// <example>
  /// var deletedConversations = await _dbContext.Conversations
  ///   .OnlyDeleted()
  ///   .ToListAsync();
  /// </example>
  public static IQueryable<TEntity> OnlyDeleted<TEntity>(this IQueryable<TEntity> query)
    where TEntity : class
  {
    // This requires the entity to have IsDeleted property
    // We use dynamic LINQ for flexibility
    return query.IgnoreQueryFilters()
                .Where(e => EF.Property<bool>(e, "IsDeleted") == true);
  }

  /// <summary>
  /// Explicitly get only non-deleted entities.
  /// Useful when you've called IgnoreQueryFilters() earlier and want to re-apply the filter.
  /// </summary>
  public static IQueryable<TEntity> OnlyActive<TEntity>(this IQueryable<TEntity> query)
    where TEntity : class
  {
    return query.Where(e => EF.Property<bool>(e, "IsDeleted") == false);
  }
}
