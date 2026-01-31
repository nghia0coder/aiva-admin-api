using Ardalis.SmartEnum;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

/// <summary>
/// Represents the detected intent of a user query in an e-commerce context.
/// Used by intent detection service to classify queries and determine appropriate response type.
/// </summary>
public sealed class QueryIntent : SmartEnum<QueryIntent>
{
  /// <summary>
  /// General conversation, policies, support questions (default)
  /// </summary>
  public static readonly QueryIntent General = new(nameof(General), 0);

  /// <summary>
  /// User wants to browse/explore products
  /// </summary>
  public static readonly QueryIntent Browse = new(nameof(Browse), 1);

  /// <summary>
  /// User wants to compare multiple products
  /// </summary>
  public static readonly QueryIntent Compare = new(nameof(Compare), 2);

  /// <summary>
  /// User shows purchase/buying intent
  /// </summary>
  public static readonly QueryIntent Purchase = new(nameof(Purchase), 3);

  /// <summary>
  /// User inquires about order status or tracking
  /// </summary>
  public static readonly QueryIntent OrderTracking = new(nameof(OrderTracking), 4);

  private QueryIntent(string name, int value) : base(name, value)
  {
  }
}
