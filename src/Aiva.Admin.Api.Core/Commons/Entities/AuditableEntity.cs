public abstract class AuditableEntity<TEntity, TId> : EntityBase<TEntity, TId> where TEntity : AuditableEntity<TEntity, TId>
{
  public DateTime CreatedOnUtc { get; private set; } = DateTime.UtcNow;
  public string? CreatedBy { get; private set; }
  public DateTime? ModifiedOnUtc { get; private set; }
  public string? ModifiedBy { get; private set; }

  public void SetAudit(string? actorId)
  {
    if (CreatedBy is null) CreatedBy = actorId;
    ModifiedBy = actorId;
    ModifiedOnUtc = DateTime.UtcNow;
  }
}
