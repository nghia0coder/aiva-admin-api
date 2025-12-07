namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.FileAggregate;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
  public void Configure(EntityTypeBuilder<FileMetadata> builder)
  {
    builder.ToTable("FileMetadata");

    builder.Property(e => e.Id)
        .HasValueGenerator<VogenIdValueGenerator<AppDbContext, FileMetadata, FileMetadataId>>()
        .HasVogenConversion()
        .IsRequired();

    builder.Property(e => e.FileId)
        .HasVogenConversion()
        .IsRequired();

    builder.HasOne<File>()
            .WithOne()
            .HasForeignKey<FileMetadata>(e => e.FileId)
            .OnDelete(DeleteBehavior.Cascade);

    builder.Property(e => e.Status)
        .HasConversion(
            status => status.Name,
            name => FileProcessingStatus.FromName(name, false))
        .HasMaxLength(20)
        .IsRequired();

    builder.Property(e => e.QueuedAt);
    builder.Property(e => e.ProcessingStartedAt);
    builder.Property(e => e.ProcessingCompletedAt);
    builder.Property(e => e.RetryCount).HasDefaultValue(0);

    builder.Property(e => e.ErrorMessage)
        .HasMaxLength(2000);

    // Extracted content - use nvarchar(max) for large text
    builder.Property(e => e.ExtractedText)
        .HasColumnType("nvarchar(max)");

    builder.Property(e => e.PageCount);
    builder.Property(e => e.WordCount);

    builder.Property(e => e.DetectedLanguage)
        .HasMaxLength(10);

    builder.Property(e => e.ContentHash)
        .HasMaxLength(128);

    builder.Property(e => e.IsEmbedded)
        .HasDefaultValue(false);

    builder.Property(e => e.EmbeddedAt);

    builder.Property(e => e.AdditionalMetadataJson)
        .HasColumnType("nvarchar(max)");

    // Indexes
    builder.HasIndex(e => e.FileId).IsUnique();  // 1:1 relationship
    builder.HasIndex(e => e.Status);
    builder.HasIndex(e => new { e.Status, e.QueuedAt });  // For processing queue queries
  }
}
