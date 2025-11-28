namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.FolderAggregate;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
  public void Configure(EntityTypeBuilder<Folder> builder)
  {
    builder.ToTable("Folders");

    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenIdValueGenerator<AppDbContext, Folder, FolderId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Name)
      .HasVogenConversion()
      .HasMaxLength(FolderName.MaxLength)
      .IsRequired();

    builder.Property(entity => entity.BlobPrefix)
      .HasMaxLength(1000)
      .IsRequired();

    builder.Property(entity => entity.Description)
      .HasMaxLength(500);

    // Foreign key to Storage (reference by ID only)
    builder.Property(entity => entity.StorageId)
      .HasVogenConversion()
      .IsRequired();

    // Self-referencing relationship for folder hierarchy
    builder.Property(entity => entity.ParentFolderId)
      .HasConversion(
        v => v.HasValue ? v.Value.Value : (int?)null,
        v => v.HasValue ? FolderId.From(v.Value) : null);

    builder.HasOne<Folder>()
      .WithMany()
      .HasForeignKey(f => f.ParentFolderId)
      .OnDelete(DeleteBehavior.Restrict);

    // Indexes for efficient queries
    builder.HasIndex(f => f.StorageId);
    builder.HasIndex(f => new { f.StorageId, f.ParentFolderId });
    builder.HasIndex(f => f.BlobPrefix);
  }
}
