using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.Interfaces;
using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.Formatting;

/// <summary>
/// Formats RAG retrieval results into structured table data for product queries
/// </summary>
public sealed class ProductTableResponseFormatterService : IResponseFormatterService
{
  private readonly ILogger<ProductTableResponseFormatterService> _logger;

  public ProductTableResponseFormatterService(ILogger<ProductTableResponseFormatterService> logger)
  {
    _logger = logger;
  }

  public async Task<Result<StructuredResponsePackage>> FormatStructuredResponseAsync(
      QueryIntent intent,
      IReadOnlyList<VectorSearchResult> retrievalResults,
      string userQuery,
      CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation(
          "Formatting structured response for intent: {Intent}, Results: {Count}",
          intent.Name,
          retrievalResults.Count);

      // Extract product data from retrieval results
      var products = ExtractProductData(retrievalResults);

      if (products.Count == 0)
      {
        _logger.LogWarning("No product data found in retrieval results");
        return Result.Error("No product data available for structured response");
      }

      // Build table structure based on intent
      var tableData = intent.Name switch
      {
        nameof(QueryIntent.Browse) => BuildBrowseTable(products),
        nameof(QueryIntent.Compare) => BuildCompareTable(products),
        nameof(QueryIntent.Purchase) => BuildPurchaseTable(products),
        _ => BuildBrowseTable(products)  // Default to browse format
      };

      // Generate chatbot suggestion text
      var suggestionText = GenerateSuggestionText(intent, products.Count, userQuery);

      var package = new StructuredResponsePackage(suggestionText, tableData)
      {
        Metadata = new Dictionary<string, object>
        {
          ["intent"] = intent.Name,
          ["productCount"] = products.Count,
          ["timestamp"] = DateTime.UtcNow
        }
      };

      return Result.Success(package);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error formatting structured response");
      return Result.Error($"Failed to format structured response: {ex.Message}");
    }
  }

  private List<ProductData> ExtractProductData(IReadOnlyList<VectorSearchResult> results)
  {
    _logger.LogDebug("Extracting product data from {ChunkCount} chunks", results.Count);

    // Group chunks by DocumentId and take only the first chunk (chunk 0) from each document
    // This ensures each product appears only once, even if multiple chunks were retrieved
    var uniqueDocuments = results
        .GroupBy(r => r.DocumentId)
        .Select(g => new
        {
          DocumentId = g.Key,
          // Take the first chunk (chunkIndex = 0) which contains the full metadata
          PrimaryChunk = g.OrderBy(r => GetChunkIndex(r.ChunkId)).FirstOrDefault(),
          ChunkCount = g.Count(),
          TopScore = g.Max(r => r.Score)
        })
        .Where(d => d.PrimaryChunk != null)
        .ToList();

    _logger.LogInformation(
        "Deduplicated {ChunkCount} chunks into {ProductCount} unique products",
        results.Count,
        uniqueDocuments.Count);

    var products = new List<ProductData>();

    foreach (var doc in uniqueDocuments)
    {
      var result = doc.PrimaryChunk!;
      var metadata = result.Metadata;

      // Extract product information from metadata
      var productId = metadata?.ProductId ?? result.DocumentId;
      var productName = metadata?.ProductName ?? metadata?.FileName ?? "Unknown Product";
      var category = metadata?.Category ?? "General";
      var brand = metadata?.Brand;
      var price = metadata?.Price.HasValue == true ? (decimal?)metadata.Price.Value : null;

      // Get rating from custom metadata if available
      double? rating = null;
      if (metadata?.CustomMetadata?.TryGetValue("rating", out var ratingStr) == true)
      {
        rating = ParseRating(ratingStr);
      }

      // Get availability from custom metadata if available
      var availability = "In Stock";
      if (metadata?.CustomMetadata?.TryGetValue("availability", out var availabilityStr) == true)
      {
        availability = availabilityStr;
      }

      // Get image URL from custom metadata if available
      string? imageUrl = null;
      if (metadata?.CustomMetadata?.TryGetValue("imageUrl", out var imageUrlStr) == true)
      {
        imageUrl = imageUrlStr;
      }

      _logger.LogDebug(
          "Product extracted: {ProductName} (DocumentId: {DocumentId}, ChunkCount: {ChunkCount}, TopScore: {Score:F4})",
          productName,
          doc.DocumentId,
          doc.ChunkCount,
          doc.TopScore);

      products.Add(new ProductData(
          productId,
          productName,
          category,
          brand,
          price,
          rating,
          availability,
          imageUrl,
          result.Content,
          doc.TopScore)); // Use the highest score among all chunks of this document
    }

    return products;
  }

  /// <summary>
  /// Extracts chunk index from chunk ID (format: "documentId_chunkIndex")
  /// </summary>
  private static int GetChunkIndex(string chunkId)
  {
    var lastUnderscore = chunkId.LastIndexOf('_');
    if (lastUnderscore >= 0 && lastUnderscore < chunkId.Length - 1)
    {
      var indexStr = chunkId[(lastUnderscore + 1)..];
      if (int.TryParse(indexStr, out var index))
      {
        return index;
      }
    }
    return 0; // Default to 0 if parsing fails
  }

  private TableData BuildBrowseTable(List<ProductData> products)
  {
    var columns = new List<TableColumn>
    {
      new("image", "Hình ảnh", ColumnType.Image, Sortable: false),
      new("name", "Tên sản phẩm", ColumnType.Text),
      new("price", "Giá", ColumnType.Currency),
      new("rating", "Đánh giá", ColumnType.Rating),
      new("brand", "Thương hiệu", ColumnType.Text),
      new("availability", "Tình trạng", ColumnType.Text, Sortable: false)
    };

    var rows = products.Select(p => new TableRow(
        p.Id,
        new Dictionary<string, object?>
        {
          ["image"] = p.ImageUrl ?? "/placeholder.png",
          ["name"] = p.Name,
          ["price"] = p.Price,
          ["rating"] = p.Rating,
          ["brand"] = p.Brand ?? "N/A",
          ["availability"] = p.Availability
        },
        new List<ActionMetadata>
        {
          new(ActionType.ViewDetail, "Xem chi tiết", "eye", $"/api/products/{p.Id}", "GET",
              new Dictionary<string, object> { ["productId"] = p.Id }),
          new(ActionType.AddToCart, "Thêm vào giỏ", "cart", "/api/cart/items", "POST",
              new Dictionary<string, object> { ["productId"] = p.Id, ["quantity"] = 1 },
              IsDisabled: p.Availability != "In Stock",
              DisabledReason: p.Availability != "In Stock" ? "Hết hàng" : null),
          new(ActionType.Compare, "So sánh", "compare", "/api/compare", "GET",
              new Dictionary<string, object> { ["productIds"] = new[] { p.Id } })
        }
    )).ToList();

    var globalActions = new List<ActionMetadata>
    {
      new(ActionType.Compare, "So sánh đã chọn", "compare", "/api/compare", "GET",
          new Dictionary<string, object>())
    };

    return new TableData
    {
      Metadata = new TableMetadata(
          "Sản phẩm phù hợp",
          "Danh sách sản phẩm dựa trên tìm kiếm của bạn",
          products.Count,
          products.Count),
      Columns = columns,
      Rows = rows,
      GlobalActions = globalActions
    };
  }

  private TableData BuildCompareTable(List<ProductData> products)
  {
    // For comparison, show more detailed specs
    var columns = new List<TableColumn>
    {
      new("image", "Hình ảnh", ColumnType.Image, Sortable: false),
      new("name", "Tên sản phẩm", ColumnType.Text),
      new("brand", "Thương hiệu", ColumnType.Text),
      new("price", "Giá", ColumnType.Currency),
      new("rating", "Đánh giá", ColumnType.Rating),
      new("availability", "Tình trạng", ColumnType.Text, Sortable: false)
    };

    var rows = products.Take(5).Select(p => new TableRow(  // Limit to 5 for comparison
        p.Id,
        new Dictionary<string, object?>
        {
          ["image"] = p.ImageUrl ?? "/placeholder.png",
          ["name"] = p.Name,
          ["brand"] = p.Brand ?? "N/A",
          ["price"] = p.Price,
          ["rating"] = p.Rating,
          ["availability"] = p.Availability
        },
        new List<ActionMetadata>
        {
          new(ActionType.ViewDetail, "Xem chi tiết", "eye", $"/api/products/{p.Id}", "GET",
              new Dictionary<string, object> { ["productId"] = p.Id }),
          new(ActionType.AddToCart, "Thêm vào giỏ", "cart", "/api/cart/items", "POST",
              new Dictionary<string, object> { ["productId"] = p.Id, ["quantity"] = 1 },
              IsDisabled: p.Availability != "In Stock")
        }
    )).ToList();

    return new TableData
    {
      Metadata = new TableMetadata(
          "So sánh sản phẩm",
          "Bảng so sánh chi tiết",
          products.Count,
          Math.Min(products.Count, 5)),
      Columns = columns,
      Rows = rows,
      GlobalActions = new List<ActionMetadata>()
    };
  }

  private TableData BuildPurchaseTable(List<ProductData> products)
  {
    // For purchase intent, emphasize "Add to Cart" action prominently
    var columns = new List<TableColumn>
    {
      new("image", "Hình ảnh", ColumnType.Image, Sortable: false),
      new("name", "Tên sản phẩm", ColumnType.Text),
      new("price", "Giá", ColumnType.Currency),
      new("rating", "Đánh giá", ColumnType.Rating),
      new("brand", "Thương hiệu", ColumnType.Text),
      new("availability", "Tình trạng", ColumnType.Text, Sortable: false)
    };

    var rows = products.Select(p => new TableRow(
        p.Id,
        new Dictionary<string, object?>
        {
          ["image"] = p.ImageUrl ?? "/placeholder.png",
          ["name"] = p.Name,
          ["price"] = p.Price,
          ["rating"] = p.Rating,
          ["brand"] = p.Brand ?? "N/A",
          ["availability"] = p.Availability
        },
        new List<ActionMetadata>
        {
          // Purchase intent: AddToCart is primary action
          new(ActionType.AddToCart, "Thêm vào giỏ hàng", "shopping-cart", "/api/cart/items", "POST",
              new Dictionary<string, object> { ["productId"] = p.Id, ["quantity"] = 1 },
              IsDisabled: p.Availability != "In Stock",
              DisabledReason: p.Availability != "In Stock" ? "Hết hàng" : null),
          new(ActionType.ViewDetail, "Xem chi tiết", "eye", $"/api/products/{p.Id}", "GET",
              new Dictionary<string, object> { ["productId"] = p.Id }),
          new(ActionType.QuickView, "Xem nhanh", "expand", $"/api/products/{p.Id}/quick", "GET",
              new Dictionary<string, object> { ["productId"] = p.Id })
        }
    )).ToList();

    var globalActions = new List<ActionMetadata>
    {
      new(ActionType.Compare, "So sánh đã chọn", "compare", "/api/compare", "GET",
          new Dictionary<string, object>()),
      new(ActionType.AddToCart, "Thêm tất cả vào giỏ", "shopping-cart", "/api/cart/bulk", "POST",
          new Dictionary<string, object> { ["productIds"] = products.Select(p => p.Id).ToArray() })
    };

    return new TableData
    {
      Metadata = new TableMetadata(
          "Sản phẩm sẵn sàng mua",
          "Chọn sản phẩm và thêm vào giỏ hàng ngay",
          products.Count,
          products.Count),
      Columns = columns,
      Rows = rows,
      GlobalActions = globalActions
    };
  }

  private string GenerateSuggestionText(QueryIntent intent, int productCount, string userQuery)
  {
    return intent.Name switch
    {
      nameof(QueryIntent.Browse) when productCount == 1 =>
          $"Tôi tìm thấy 1 sản phẩm phù hợp. Bạn có thể xem chi tiết hoặc thêm vào giỏ hàng.",

      nameof(QueryIntent.Browse) when productCount <= 5 =>
          $"Tôi tìm thấy {productCount} sản phẩm phù hợp với yêu cầu của bạn. Bạn có thể xem chi tiết từng sản phẩm hoặc thêm ngay vào giỏ hàng.",

      nameof(QueryIntent.Browse) =>
          $"Tôi tìm thấy {productCount} sản phẩm phù hợp. Bạn có thể so sánh thông số, xem đánh giá, hoặc thêm sản phẩm yêu thích vào giỏ hàng.",

      nameof(QueryIntent.Compare) when productCount == 2 =>
          $"Đây là so sánh chi tiết giữa 2 sản phẩm. Tôi đã tổng hợp các thông số quan trọng để bạn dễ ra quyết định.",

      nameof(QueryIntent.Compare) =>
          $"Tôi đã so sánh {Math.Min(productCount, 5)} sản phẩm theo các tiêu chí bạn quan tâm. Bạn có thể xem chi tiết từng sản phẩm hoặc hỏi tôi về bất kỳ tính năng nào.",

      nameof(QueryIntent.Purchase) when productCount == 1 =>
          $"Tôi tìm thấy 1 sản phẩm phù hợp với nhu cầu của bạn. Bạn có thể xem chi tiết và thêm ngay vào giỏ hàng nếu thích.",

      nameof(QueryIntent.Purchase) when productCount <= 5 =>
          $"Tôi tìm thấy {productCount} sản phẩm phù hợp. Bạn có thể xem thông tin chi tiết và thêm sản phẩm yêu thích vào giỏ hàng để thanh toán.",

      nameof(QueryIntent.Purchase) =>
          $"Tôi tìm thấy {productCount} sản phẩm cho bạn. Hãy xem qua và click 'Thêm vào giỏ' để mua sản phẩm bạn thích nhất!",

      _ => $"Tôi tìm thấy {productCount} sản phẩm có thể phù hợp. Hãy xem chi tiết và cho tôi biết nếu bạn cần thêm thông tin!"
    };
  }

  private decimal? ParsePrice(string? priceString)
  {
    if (string.IsNullOrWhiteSpace(priceString))
      return null;

    // Remove currency symbols and commas
    var cleaned = priceString
        .Replace("VND", "")
        .Replace("đ", "")
        .Replace("₫", "")
        .Replace("$", "")
        .Replace(",", "")
        .Trim();

    return decimal.TryParse(cleaned, out var price) ? price : null;
  }

  private double? ParseRating(string? ratingString)
  {
    if (string.IsNullOrWhiteSpace(ratingString))
      return null;

    return double.TryParse(ratingString, out var rating) ? rating : null;
  }

  /// <summary>
  /// Internal product data structure extracted from retrieval results
  /// </summary>
  private sealed record ProductData(
      string Id,
      string Name,
      string Category,
      string? Brand,
      decimal? Price,
      double? Rating,
      string Availability,
      string? ImageUrl,
      string Description,
      double RelevanceScore);
}
