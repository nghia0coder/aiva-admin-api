using Aiva.Admin.Api.Core.Commons.Interfaces;

public abstract class AuditableEntity<TEntity, TId> : EntityBase<TEntity, TId>, ISoftDeletable 
  where TEntity : AuditableEntity<TEntity, TId>
{
  public DateTime CreatedOnUtc { get; private set; } = DateTime.UtcNow;
  public string? CreatedBy { get; private set; }
  public DateTime? ModifiedOnUtc { get; private set; }
  public string? ModifiedBy { get; private set; }
  public bool IsDeleted { get; private set; } = false;
  public DateTime? DeletedAt { get; private set; }

  public void SetAudit(string? actorId)
  {
    if (CreatedBy is null) CreatedBy = actorId;
    ModifiedBy = actorId;
    ModifiedOnUtc = DateTime.UtcNow;
  }

  protected void MarkAsDeleted()
  {
    if (IsDeleted) return;
    IsDeleted = true;
    DeletedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Restore a soft-deleted entity
  /// </summary>
  protected void Restore()
  {
    if (!IsDeleted) return;
    IsDeleted = false;
    DeletedAt = null;
  }
}
