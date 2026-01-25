namespace Aiva.Admin.Api.Core.Commons.Interfaces;

/// <summary>
/// Marker interface for entities that support soft delete
/// </summary>
public interface ISoftDeletable
{
  /// <summary>
  /// Indicates whether this entity has been soft deleted
  /// </summary>
  bool IsDeleted { get; }

  /// <summary>
  /// The timestamp when this entity was soft deleted
  /// </summary>
  DateTime? DeletedAt { get; }
}
