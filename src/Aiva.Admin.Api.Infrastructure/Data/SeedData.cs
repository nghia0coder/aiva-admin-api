using Aiva.Admin.Api.Core.ContributorAggregate;
using Aiva.Admin.Api.Core.SystemPromptAggregate;

namespace Aiva.Admin.Api.Infrastructure.Data;

public static class SeedData
{
  public const int NUMBER_OF_CONTRIBUTORS = 27; // including the 2 below
  public static readonly Contributor Contributor1 = new(ContributorName.From("Ardalis"));
  public static readonly Contributor Contributor2 = new(ContributorName.From("Ilyana"));

  public static async Task InitializeAsync(AppDbContext dbContext)
  {
    if (await dbContext.Contributors.AnyAsync()) return; // DB has been seeded

    await PopulateTestDataAsync(dbContext);
    await SeedSystemPromptsAsync(dbContext);
  }

  public static async Task PopulateTestDataAsync(AppDbContext dbContext)
  {
    dbContext.Contributors.AddRange([Contributor1, Contributor2]);
    await dbContext.SaveChangesAsync();

    // add a bunch more contributors to support demonstrating paging
    for (int i = 1; i <= NUMBER_OF_CONTRIBUTORS - 2; i++)
    {
      dbContext.Contributors.Add(new Contributor(ContributorName.From($"Contributor {i}")));
    }
    await dbContext.SaveChangesAsync();
  }

  public static async Task SeedSystemPromptsAsync(AppDbContext context)
  {
    if (await context.Set<SystemPrompt>().AnyAsync())
      return; // Already seeded

    var defaultPrompt = SystemPrompt.Create(
        SystemPromptKey.From("default"),
        "Default System Prompt",
        "You are a helpful AI assistant for the Aiva Admin system. Provide clear, accurate, and professional responses.",
        description: "Default system prompt for general conversations");
    defaultPrompt.Activate();

    var customerSupportPrompt = SystemPrompt.Create(
        SystemPromptKey.From("customer-support"),
        "Customer Support Assistant",
        @"You are a customer support AI assistant for an e-commerce platform. Your role is to:
        - Help customers track their orders
        - Answer product questions
        - Assist with returns and refunds
        - Provide shipping information
        Be friendly, professional, and always prioritize customer satisfaction.",
        description: "System prompt for customer-facing chatbot");
    customerSupportPrompt.Activate();

    context.AddRange(defaultPrompt, customerSupportPrompt);
    await context.SaveChangesAsync();
  }
}
