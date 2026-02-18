namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

/// <summary>
/// Represents structured data extracted from HTML table sent by frontend
/// </summary>
public class ProductSelectionData
{
  public List<SelectedProductItem> SelectedProducts { get; set; } = new();
  public List<string> UnselectedProducts { get; set; } = new();
  public string RawHtml { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single product item selected by user
/// </summary>
public class SelectedProductItem
{
  public string ProductId { get; set; } = string.Empty;
  public string ProductName { get; set; } = string.Empty;
  public int Quantity { get; set; } = 1;
  public bool IsChecked { get; set; }
  public string ProductUrl { get; set; } = string.Empty;
  public decimal? Price { get; set; }
  public Dictionary<string, string> Attributes { get; set; } = new();
}
