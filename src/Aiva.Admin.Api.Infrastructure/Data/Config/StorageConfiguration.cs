namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.StorageAggregate;

public class StorageConfiguration : IEntityTypeConfiguration<Storage>
{
  public void Configure(EntityTypeBuilder<Storage> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenIdValueGenerator<AppDbContext, Storage, StorageId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.StorageName)
      .HasVogenConversion()
      .HasMaxLength(StorageName.MaxLength)
      .IsRequired();
  }
}
