using Aiva.Admin.Api.Core.UserAggregate;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.ToTable("Users");

    builder.Property(e => e.Id)
        .HasValueGenerator<VogenIdValueGenerator<AppDbContext, User, UserId>>()
        .HasVogenConversion()
        .IsRequired();

    builder.Property(e => e.AzureAdObjectId)
        .HasVogenConversion()
        .HasMaxLength(36)
        .IsRequired();

    // Unique index on Azure AD Object ID - critical for lookups
    builder.HasIndex(e => e.AzureAdObjectId)
        .IsUnique()
        .HasDatabaseName("IX_Users_AzureAdObjectId");

    builder.Property(e => e.Email)
        .HasMaxLength(256)
        .IsRequired();

    builder.HasIndex(e => e.Email)
        .HasDatabaseName("IX_Users_Email");

    builder.Property(e => e.DisplayName)
        .HasMaxLength(256)
        .IsRequired();

    builder.Property(e => e.FirstName)
        .HasMaxLength(100);

    builder.Property(e => e.LastName)
        .HasMaxLength(100);

    builder.Property(e => e.Status)
        .HasConversion(
            x => x.Value,
            x => UserStatus.FromValue(x));

    builder.Property(e => e.Role)
        .HasConversion(
            x => x.Value,
            x => UserRole.FromValue(x));
  }
}
