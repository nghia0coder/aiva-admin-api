using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.UserAggregate;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
  public void Configure(EntityTypeBuilder<Conversation> builder)
  {
    builder.ToTable("Conversations");

    builder.HasKey(c => c.Id);

    builder.Property(c => c.Id)
        .HasConversion(
            id => id.Value,
            value => ConversationId.From(value));

    builder.Property(c => c.UserId)
    .HasConversion(
        v => v.Value,
        v => UserId.From(v))
    .IsRequired();

    builder.Property(c => c.Title)
        .HasMaxLength(200)
        .IsRequired();

    builder.Property(c => c.SystemPrompt)
        .HasMaxLength(4000);

    builder.Property(c => c.CreatedAt)
        .IsRequired();

    builder.HasMany(c => c.Messages)
        .WithOne()
        .HasForeignKey(m => m.ConversationId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Property(c => c.TitleStatus)
        .HasConversion<int>()
        .HasDefaultValue(TitleGenerationStatus.Pending)
        .IsRequired();

    builder.HasIndex(c => c.TitleStatus);

    builder.Navigation(c => c.Messages)
        .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(c => c.UserId);
    builder.HasIndex(c => new { c.UserId, c.LastMessageAt });
  }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
  public void Configure(EntityTypeBuilder<ChatMessage> builder)
  {
    builder.ToTable("ChatMessages");

    builder.HasKey(m => m.Id);

    builder.Property(m => m.Id)
        .HasConversion(
            id => id.Value,
            value => MessageId.From(value));

    builder.Property(m => m.ConversationId)
        .HasConversion(
            id => id.Value,
            value => ConversationId.From(value));

    builder.Property(m => m.Role)
        .HasConversion(
            role => role.Name,
            name => ChatRole.FromName(name, false))
        .HasMaxLength(20)
        .IsRequired();

    builder.Property(m => m.Content)
        .IsRequired();

    builder.Property(m => m.CreatedAt)
        .IsRequired();

    builder.HasIndex(m => m.ConversationId);
  }
}
