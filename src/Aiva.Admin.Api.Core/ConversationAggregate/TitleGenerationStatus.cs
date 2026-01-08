namespace Aiva.Admin.Api.Core.ConversationAggregate;

public enum TitleGenerationStatus
{
  Pending,      // Default for new conversations
  Queued,       // Queued for background processing
  Generated,    // Title has been auto-generated
  Manual        // Title was manually set by user
}
