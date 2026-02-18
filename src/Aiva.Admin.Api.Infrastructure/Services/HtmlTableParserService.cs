using System.Text.RegularExpressions;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class HtmlTableParserService : IHtmlTableParserService
{
  private readonly ILogger<HtmlTableParserService> _logger;

  public HtmlTableParserService(ILogger<HtmlTableParserService> logger)
  {
    _logger = logger;
  }

  public ProductSelectionData ParseProductTable(string htmlTable)
  {
    var result = new ProductSelectionData
    {
      RawHtml = htmlTable
    };

    if (string.IsNullOrWhiteSpace(htmlTable))
    {
      return result;
    }

    try
    {
      // Extract all <tr> rows from tbody
      var rowPattern = @"<tr[^>]*>(.*?)</tr>";
      var rowMatches = Regex.Matches(htmlTable, rowPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

      foreach (Match rowMatch in rowMatches)
      {
        var rowHtml = rowMatch.Groups[1].Value;

        // Skip header rows (those containing <th>)
        if (rowHtml.Contains("<th", StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        var item = ParseProductRow(rowHtml);
        if (item != null)
        {
          if (item.IsChecked)
          {
            result.SelectedProducts.Add(item);
          }
          else
          {
            result.UnselectedProducts.Add(item.ProductId);
          }
        }
      }

      _logger.LogInformation("Parsed {SelectedCount} selected products and {UnselectedCount} unselected products",
          result.SelectedProducts.Count, result.UnselectedProducts.Count);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error parsing HTML table");
    }

    return result;
  }

  private SelectedProductItem? ParseProductRow(string rowHtml)
  {
    try
    {
      var item = new SelectedProductItem();

      // Parse checkbox state and value (product ID)
      var checkboxPattern = @"<input[^>]*type=[""']checkbox[""'][^>]*>";
      var checkboxMatch = Regex.Match(rowHtml, checkboxPattern, RegexOptions.IgnoreCase);
      if (checkboxMatch.Success)
      {
        var checkboxHtml = checkboxMatch.Value;
        item.IsChecked = checkboxHtml.Contains("checked", StringComparison.OrdinalIgnoreCase);

        // Extract product ID from checkbox value attribute
        var valuePattern = @"value=[""']([^""']+)[""']";
        var valueMatch = Regex.Match(checkboxHtml, valuePattern, RegexOptions.IgnoreCase);
        if (valueMatch.Success)
        {
          item.ProductId = valueMatch.Groups[1].Value;
        }
      }

      // Parse product link and URL (fallback for product ID if not found in checkbox)
      var linkPattern = @"<a[^>]*href=[""']([^""']+)[""'][^>]*>([^<]+)</a>";
      var linkMatch = Regex.Match(rowHtml, linkPattern, RegexOptions.IgnoreCase);
      if (linkMatch.Success)
      {
        var href = linkMatch.Groups[1].Value;
        item.ProductName = linkMatch.Groups[2].Value.Trim();
        item.ProductUrl = href;

        // Only use URL-based ID if we don't already have one from checkbox
        if (string.IsNullOrWhiteSpace(item.ProductId))
        {
          item.ProductId = ExtractProductIdFromUrl(href);
        }
      }

      // Parse quantity input
      var quantityPattern = @"<input[^>]*type=[""']number[""'][^>]*value=[""'](\d+)[""'][^>]*>";
      var quantityMatch = Regex.Match(rowHtml, quantityPattern, RegexOptions.IgnoreCase);
      if (quantityMatch.Success && int.TryParse(quantityMatch.Groups[1].Value, out var quantity))
      {
        item.Quantity = quantity;
      }

      // Parse price (optional)
      var pricePattern = @"\$(\d+(?:\.\d+)?)";
      var priceMatch = Regex.Match(rowHtml, pricePattern);
      if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, out var price))
      {
        item.Price = price;
      }

      // Parse data attributes if present (override product ID if explicitly set)
      var dataProductIdPattern = @"data-product-id=[""']([^""']+)[""']";
      var dataProductIdMatch = Regex.Match(rowHtml, dataProductIdPattern, RegexOptions.IgnoreCase);
      if (dataProductIdMatch.Success)
      {
        item.ProductId = dataProductIdMatch.Groups[1].Value;
      }

      // Parse cart item ID if present (for cart tables)
      var dataCartItemIdPattern = @"data-cart-item-id=[""']([^""']+)[""']";
      var dataCartItemIdMatch = Regex.Match(rowHtml, dataCartItemIdPattern, RegexOptions.IgnoreCase);
      if (dataCartItemIdMatch.Success)
      {
        item.Attributes["CartItemId"] = dataCartItemIdMatch.Groups[1].Value;
      }

      // Only return if we have at least a product identifier
      if (!string.IsNullOrWhiteSpace(item.ProductId) || !string.IsNullOrWhiteSpace(item.ProductName))
      {
        return item;
      }

      return null;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error parsing product row: {RowHtml}", rowHtml.Substring(0, Math.Min(100, rowHtml.Length)));
      return null;
    }
  }

  private string ExtractProductIdFromUrl(string url)
  {
    // Remove leading/trailing slashes and extract the last segment
    var segments = url.Trim('/').Split('/');
    return segments.Last();
  }
}
