namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.FileAggregate;

public class FileConfiguration : IEntityTypeConfiguration<File>
{
  public void Configure(EntityTypeBuilder<File> builder)
  {
    builder.ToTable("Files");

    builder.Property(e => e.Id)
        .HasValueGenerator<VogenIdValueGenerator<AppDbContext, File, FileId>>()
        .HasVogenConversion()
        .IsRequired();

    builder.Property(e => e.OriginalFileName)
        .HasVogenConversion()
        .HasMaxLength(FileName.MaxLength)
        .IsRequired();

    builder.Property(e => e.StoredFileName)
        .HasMaxLength(300)
        .IsRequired();

    builder.Property(e => e.Extension)
        .HasMaxLength(10)
        .IsRequired();

    builder.Property(e => e.ContentType)
        .HasMaxLength(100)
        .IsRequired();

    builder.Property(e => e.FileSizeBytes)
        .IsRequired();

    builder.Property(e => e.BlobPath)
        .HasMaxLength(1000)
        .IsRequired();

    builder.Property(e => e.BlobUrl)
        .HasMaxLength(2000)
        .IsRequired();

    builder.Property(e => e.StorageId)
        .HasVogenConversion()
        .IsRequired();

    builder.Property(e => e.FolderId)
        .HasVogenConversion()
        .IsRequired();

    // Indexes
    builder.HasIndex(e => e.StorageId);
    builder.HasIndex(e => e.FolderId);
    builder.HasIndex(e => new { e.StorageId, e.FolderId });
  }
}
