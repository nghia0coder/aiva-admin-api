using Aiva.Admin.Api.Core.SystemPromptAggregate;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

public class SystemPromptConfiguration : IEntityTypeConfiguration<SystemPrompt>
{
  public void Configure(EntityTypeBuilder<SystemPrompt> builder)
  {
    builder.ToTable("SystemPrompts");

    builder.Property(sp => sp.Id)
        .HasValueGenerator<VogenIdValueGenerator<AppDbContext, SystemPrompt, SystemPromptId>>()
        .HasVogenConversion()
        .IsRequired();

    builder.Property(sp => sp.Key)
        .HasConversion(
            k => k.Value,
            v => SystemPromptKey.From(v))
        .HasMaxLength(SystemPromptKey.MaxLength)
        .IsRequired();

    builder.HasIndex(sp => sp.Key)
        .HasDatabaseName("IX_SystemPrompts_Key");

    builder.Property(sp => sp.Name)
        .HasMaxLength(200)
        .IsRequired();

    builder.Property(sp => sp.Content)
        .HasMaxLength(8000)
        .IsRequired();

    builder.Property(sp => sp.Description)
        .HasMaxLength(1000);

    builder.Property(sp => sp.Version)
        .IsRequired()
        .HasDefaultValue(1);

    builder.Property(sp => sp.IsActive)
        .IsRequired()
        .HasDefaultValue(false);

    builder.HasIndex(sp => new { sp.Key, sp.IsActive })
        .HasDatabaseName("IX_SystemPrompts_Key_IsActive");

    builder.HasIndex(sp => sp.IsActive)
        .HasDatabaseName("IX_SystemPrompts_IsActive");
  }
}
