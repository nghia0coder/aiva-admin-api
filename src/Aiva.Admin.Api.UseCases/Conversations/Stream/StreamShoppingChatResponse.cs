namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamShoppingChatResponse
{
  public string TextResponse { get; set; } = string.Empty;
  public bool HasProducts { get; set; }
  public List<string> ToolsExecuted { get; set; } = new();
  public bool HasToolsExecuted => ToolsExecuted.Any();
  public string? AdditionalData { get; set; }
}

// TODO: Implement these models based on your business requirements
public class ProductRecommendation
{
  public string ProductName { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string Description { get; set; } = string.Empty;
  public string ImageUrl { get; set; } = string.Empty;
  public double Rating { get; set; }
}

public class PriceComparison
{
  public string ProductName { get; set; } = string.Empty;
  public List<PriceOption> Prices { get; set; } = new();
}

public class PriceOption
{
  public string StoreName { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string Url { get; set; } = string.Empty;
}
