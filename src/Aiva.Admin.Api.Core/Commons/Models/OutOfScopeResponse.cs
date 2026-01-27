namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Provides standardized out-of-scope response messages for grounded RAG.
/// Used when no relevant context is found in the knowledge base.
/// </summary>
public static class OutOfScopeResponse
{
  /// <summary>
  /// Default out-of-scope response message (English)
  /// </summary>
  public static string Default =>
      "Sorry, the system currently has no information about this product or topic. " +
      "Please contact the support team for further assistance.";

  /// <summary>
  /// Gets a category-specific out-of-scope response message
  /// </summary>
  /// <param name="category">The category of the query (product, order, policy, etc.)</param>
  /// <returns>A localized out-of-scope message for the category</returns>
  public static string ForCategory(string? category) => category?.ToLowerInvariant() switch
  {
    "product" => "Sorry, the system currently has no information about this product.",
    "order" => "Sorry, I could not find the order information. Please check the order ID again.",
    "policy" => "Sorry, I could not find the relevant policy. Please contact customer support.",
    "shipping" => "Sorry, I could not find the related shipping information.",
    "warranty" => "Sorry, I could not find the warranty information for this product.",
    _ => Default
  };
}
