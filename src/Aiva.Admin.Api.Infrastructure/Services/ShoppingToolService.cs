using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Infrastructure.Configuration;
using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ShoppingToolService(
    HttpClient httpClient,
    IRetrievalService retrievalService,
    IRetrievalSettings retrievalSettings,
    ShoppingApiConfiguration shoppingApiConfig,
    ILogger<ShoppingToolService> logger) : IShoppingToolService
{
  private readonly ShoppingApiConfiguration _shoppingApiConfig = shoppingApiConfig;
  private readonly IRetrievalSettings _retrievalSettings = retrievalSettings;
  public IEnumerable<ToolDefinition> GetAvailableTools()
  {
    return new[]
    {
            CreateSearchProductsTool(),
            CreateGetProductInfoTool(),
            CreateAddToCartTool(),
            CreateRemoveFromCartTool(),
            CreateGetCartTool(),
            CreateCheckoutTool()
        };
  }

  public async Task<Result<string>> ExecuteToolAsync(
      string functionName,
      Dictionary<string, object> parameters,
      string userId,
      CancellationToken cancellationToken = default)
  {
    try
    {
      return functionName switch
      {
        "search_infors" => await ExecuteSearchInfoAsync(parameters, cancellationToken),
        "get_product_info" => await ExecuteGetProductInfoAsync(parameters, cancellationToken),
        "add_to_cart" => await ExecuteAddToCartAsync(parameters, userId, cancellationToken),
        "get_cart" => await ExecuteGetCartAsync(cancellationToken),
        "remove_from_cart" => await ExecuteRemoveFromCartAsync(parameters, userId, cancellationToken),
        "checkout" => await ExecuteCheckoutAsync(userId, cancellationToken),
        _ => Result.Error($"Unknown function: {functionName}")
      };
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error executing tool {FunctionName}", functionName);
      return Result.Error($"Tool execution failed: {ex.Message}");
    }
  }

  #region -- Tool Definitions --
  private static ToolDefinition CreateAddToCartTool()
  {
    return new ToolDefinition
    {
      Function = new FunctionDefinition
      {
        Name = "add_to_cart",
        Description = "Add a product to the user's shopping cart",
        Parameters = new
        {
          type = "object",
          properties = new
          {
            product_id = new { type = "string", description = "The ID of the product to add" },
            quantity = new { type = "integer", description = "Quantity to add (default: 1)" },
            size = new { type = "string", description = "Product size if applicable (legacy)" },
            color = new { type = "string", description = "Product color if applicable (legacy)" },
            search_attributes = new
            {
              type = "array",
              description = "Product attributes from [additional_data].extraData.searchAttributes. Each item: { name, value }",
              items = new { type = "object", properties = new { name = new { type = "string" }, value = new { type = "string" } } }
            }
          },
          required = new[] { "product_id", "product_name" }
        }
      }
    };
  }

  private static ToolDefinition CreateSearchProductsTool()
  {
    return new ToolDefinition
    {
      Function = new FunctionDefinition
      {
        Name = "search_infors",
        Description = "Search for products in the database using Azure AI Search. Use this when user asks to find, search, look for products, or needs product information.",
        Parameters = new
        {
          type = "object",
          properties = new
          {
            query = new { type = "string", description = "Search query for products (product name, keywords, description, category, etc.)" },
            category = new { type = "string", description = "Product category filter (optional)" },
            price_min = new { type = "number", description = "Minimum price filter (optional)" },
            price_max = new { type = "number", description = "Maximum price filter (optional)" }
          },
          required = new[] { "query" }
        }
      }
    };
  }

  private static ToolDefinition CreateGetProductInfoTool()
  {
    return new ToolDefinition
    {
      Function = new FunctionDefinition
      {
        Name = "get_product_info",
        Description = "Get detailed information about a specific product using its ID. Use when user asks for details about a specific product they already know.",
        Parameters = new
        {
          type = "object",
          properties = new
          {
            product_id = new { type = "string", description = "The exact ID of the product to get detailed information for" }
          },
          required = new[] { "product_id" }
        }
      }
    };
  }

  private static ToolDefinition CreateRemoveFromCartTool()
  {
    return new ToolDefinition
    {
      Function = new FunctionDefinition
      {
        Name = "remove_from_cart",
        Description = "Remove a product from the user's shopping cart",
        Parameters = new
        {
          type = "object",
          properties = new
          {
            product_id = new { type = "string", description = "The ID of the product to remove" },
            product_name = new { type = "string", description = "The name of the product to remove" },
            quantity = new { type = "integer", description = "Quantity to remove" }
          },
          required = new[] { "product_id", "product_name" }
        }
      }
    };
  }

  private static ToolDefinition CreateCheckoutTool()
  {
    return new ToolDefinition
    {
      Function = new FunctionDefinition
      {
        Name = "checkout",
        Description = "Initiate checkout process when user explicitly wants to complete their purchase. Use when user says 'checkout', 'thanh toán', 'đặt hàng', 'mua luôn', 'proceed to checkout', or similar checkout intentions.",
        Parameters = new
        {
          type = "object",
          properties = new { },
          required = Array.Empty<string>()
        }
      }
    };
  }

  private static ToolDefinition CreateGetCartTool()
  {
    return new ToolDefinition
    {
      Function = new FunctionDefinition
      {
        Name = "get_cart",
        Description = "Get user's shopping cart or wishlist with full product details (name, price, URL). Use when user asks about cart, 'what's in my cart', 'cart info', 'giỏ hàng', 'xem giỏ'.",
        Parameters = new
        {
          type = "object",
          properties = new { },
          required = Array.Empty<string>()
        }
      }
    };
  }

  #endregion

  #region -- Tool Execution Methods --

  private async Task<Result<string>> ExecuteCheckoutAsync(
    string userId,
    CancellationToken cancellationToken)
  {
    try
    {
      logger.LogInformation("Initiating checkout for user {UserId}", userId);

      // Return a special marker that ShoppingChatService will detect
      // to trigger redirect action
      var checkoutMessage = "🛒 [CHECKOUT_ACTION] Redirecting to checkout page...\n" +
                          "URL: https://smartstore-demo-bnf3hzhpdvbkabad.southeastasia-01.azurewebsites.net\n" +
                          "Your cart items are ready for purchase!";

      return await Task.FromResult(Result.Success(checkoutMessage));
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error initiating checkout for user {UserId}", userId);
      return Result.Error($"Checkout failed: {ex.Message}");
    }
  }

  private async Task<Result<string>> ExecuteAddToCartAsync(
      Dictionary<string, object> parameters,
      string userId,
      CancellationToken cancellationToken)
  {
    try
    {
      var productId = GetStringValue(parameters["product_id"])!;
      var productName = GetStringValue(parameters.GetValueOrDefault("product_name")) ?? $"Product #{productId}";
      var quantity = GetIntValue(parameters.GetValueOrDefault("quantity"), 1);

      var searchAttributes = ParseSearchAttributes(parameters.GetValueOrDefault("search_attributes"));

      // Convert productId to integer for OData API
      if (!int.TryParse(productId, out var productIdInt))
      {
        logger.LogWarning("Invalid productId format: {ProductId}. Expected integer.", productId);
        return Result.Error($"Invalid productId format: {productId}. Expected integer.");
      }

      var extraData = searchAttributes.Count > 0
          ? CreateExtraDataFromSearchAttributes(searchAttributes)
          : null;

      // Create OData request body - OData [FromODataBody] may expect string values for some parameters
      var requestBody = new
      {
        customerId = "6",
        productId = productIdInt.ToString(),
        quantity = quantity.ToString(),
        shoppingCartType = "1", // 1 for shopping cart, 2 for wishlist
        storeId = "0",
        extraData = extraData
      };

      var json = JsonSerializer.Serialize(requestBody);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      using var request = new HttpRequestMessage(HttpMethod.Post, _shoppingApiConfig.Endpoints.AddToCart)
      {
        Content = content
      };

      AddAuthenticationHeader(request);

      var response = await httpClient.SendAsync(request, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        logger.LogInformation("Successfully added product {ProductId} ({ProductName}) to cart for user {UserId}",
            productId, productName, userId);

        var successMessage = BuildAddToCartSuccessMessage(productName, productId, quantity, searchAttributes);
        return Result.Success(successMessage);
      }

      var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning("Failed to add product to cart. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
      return Result.Error($"Failed to add product to cart: {response.StatusCode}");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling add to cart API");
      return Result.Error($"Add to cart failed: {ex.Message}");
    }
  }

  private async Task<Result<string>> ExecuteGetCartAsync(
    CancellationToken cancellationToken)
  {
    try
    {
      var cartItems = await FetchCartItemsAsync(cancellationToken);
      if (cartItems == null || cartItems.Count == 0)
      {
        return Result.Success("Your cart is empty.");
      }

      var output = new StringBuilder();
      output.AppendLine("=== Shopping Cart ===");

      decimal totalCartValue = 0;
      int totalItems = cartItems.Count;
      int totalQuantity = cartItems.Sum(x => x.Quantity);

      foreach (var item in cartItems)
      {
        var productId = item.ProductId.ToString();
        var productName = !string.IsNullOrWhiteSpace(item.ProductName) ? item.ProductName : $"Product #{productId}";
        var quantity = item.Quantity;

        // Use pre-calculated values from API response
        var basePrice = item.BasePrice;
        var attributeAdjustments = item.AttributeAdjustments;
        var unitPrice = item.UnitPrice;
        var subtotal = item.Subtotal;

        totalCartValue += subtotal;

        output.AppendLine();
        output.AppendLine($"🛒 {productName}");
        output.AppendLine($"   • Product ID: {productId}");
        if (!string.IsNullOrWhiteSpace(item.ProductSku))
        {
          output.AppendLine($"   • SKU: {item.ProductSku}");
        }
        output.AppendLine($"   • Quantity: {quantity}");
        output.AppendLine($"   • Base Price: ${basePrice:F2}");

        if (attributeAdjustments != 0)
        {
          output.AppendLine($"   • Price Adjustment: ${attributeAdjustments:+0.00;-0.00}");
          output.AppendLine($"   • Unit Price: ${unitPrice:F2}");
        }
        else if (basePrice > 0)
        {
          output.AppendLine($"   • Unit Price: ${unitPrice:F2}");
        }

        // Display selected attributes if any
        if (item.SelectedAttributes?.Count > 0)
        {
          output.AppendLine("   • Selected Options:");
          foreach (var attribute in item.SelectedAttributes)
          {
            if (!string.IsNullOrWhiteSpace(attribute.AttributeName))
            {
              var attributeValues = attribute.Values?.Where(v => !string.IsNullOrWhiteSpace(v.Name)).ToList();
              if (attributeValues?.Count > 0)
              {
                var valueNames = string.Join(", ", attributeValues.Select(v =>
                {
                  var valueName = v.Name;
                  if (v.PriceAdjustment != 0)
                  {
                    valueName += $" ({v.PriceAdjustment:+$0.00;-$0.00})";
                  }
                  return valueName;
                }));
                output.AppendLine($"     - {attribute.AttributeName}: {valueNames}");
              }
            }
          }
        }

        output.AppendLine($"   • Subtotal: ${subtotal:F2}");
        output.AppendLine($"   • Added: {item.CreatedOnUtc:MMM dd, yyyy HH:mm} UTC");
        output.AppendLine();
      }

      output.AppendLine("═══════════════════════");
      output.AppendLine($"Cart Summary:");
      output.AppendLine($"   • Total Items: {totalItems}");
      output.AppendLine($"   • Total Quantity: {totalQuantity}");
      output.AppendLine($"   • Total Cart Value: ${totalCartValue:F2}");

      return Result.Success(output.ToString());
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error executing get ecom cart");
      return Result.Error($"Get cart failed: {ex.Message}");
    }
  }

  private async Task<List<ShoppingCartItemWithAttributes>> FetchCartItemsAsync(CancellationToken cancellationToken)
  {
    var endpoint = _shoppingApiConfig.Endpoints.GetCart;
    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
    AddAuthenticationHeader(request);

    var response = await httpClient.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
      return new List<ShoppingCartItemWithAttributes>();

    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    // Deserialize to the wrapper response object that contains Items, TotalItems, etc.
    var cartResponse = JsonSerializer.Deserialize<ShoppingCartWithAttributesResponse>(json, options);
    return cartResponse?.Items ?? new List<ShoppingCartItemWithAttributes>();
  }

  /// <summary>
  /// Represents a shopping cart response with items and total price.
  /// </summary>
  public class ShoppingCartWithAttributesResponse
  {
    /// <summary>
    /// Gets or sets the shopping cart items.
    /// </summary>
    public List<ShoppingCartItemWithAttributes> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items in the cart.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the total quantity of all items.
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Gets or sets the cart subtotal (sum of all item subtotals).
    /// </summary>
    public decimal CartSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string? CurrencyCode { get; set; }
  }

  /// <summary>
  /// Represents a shopping cart item with parsed product variant attributes.
  /// </summary>
  public class ShoppingCartItemWithAttributes
  {
    /// <summary>
    /// Gets or sets the shopping cart item identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product identifier.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Gets or sets the product SKU.
    /// </summary>
    public string? ProductSku { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the base product price (without attribute adjustments).
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Gets or sets the total price adjustments from selected attributes.
    /// </summary>
    public decimal AttributeAdjustments { get; set; }

    /// <summary>
    /// Gets or sets the unit price (base price + attribute adjustments).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the subtotal (unit price × quantity).
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the customer entered price.
    /// </summary>
    public decimal CustomerEnteredPrice { get; set; }

    /// <summary>
    /// Gets or sets the shopping cart type identifier.
    /// </summary>
    public int ShoppingCartTypeId { get; set; }

    /// <summary>
    /// Gets or sets the shopping cart type.
    /// </summary>
    public string? ShoppingCartType { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was created.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was updated.
    /// </summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the selected product variant attributes.
    /// </summary>
    public List<SelectedProductAttribute> SelectedAttributes { get; set; } = new();
  }

  /// <summary>
  /// Represents a selected product variant attribute.
  /// </summary>
  public class SelectedProductAttribute
  {
    /// <summary>
    /// Gets or sets the product variant attribute identifier.
    /// </summary>
    public int ProductVariantAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the product attribute identifier.
    /// </summary>
    public int ProductAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the attribute name (e.g., "Color", "Storage", "RAM").
    /// </summary>
    public string? AttributeName { get; set; }

    /// <summary>
    /// Gets or sets the selected attribute values.
    /// </summary>
    public List<SelectedAttributeValue> Values { get; set; } = new();
  }

  /// <summary>
  /// Represents a selected product variant attribute value.
  /// </summary>
  public class SelectedAttributeValue
  {
    /// <summary>
    /// Gets or sets the product variant attribute value identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the value name (e.g., "Rose", "64GB", "8GB").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the color (if applicable).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the price adjustment.
    /// </summary>
    public decimal PriceAdjustment { get; set; }

    /// <summary>
    /// Gets or sets the weight adjustment.
    /// </summary>
    public decimal WeightAdjustment { get; set; }
  }



  private static List<SearchAttributeDto> ParseSearchAttributes(object? value)
  {
    if (value == null)
      return new List<SearchAttributeDto>();

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    try
    {
      if (value is JsonElement jsonElement)
      {
        var json = jsonElement.GetRawText();
        var list = JsonSerializer.Deserialize<List<SearchAttributeDto>>(json, options);
        return list ?? new List<SearchAttributeDto>();
      }

      if (value is string str && !string.IsNullOrWhiteSpace(str))
      {
        var list = JsonSerializer.Deserialize<List<SearchAttributeDto>>(str, options);
        return list ?? new List<SearchAttributeDto>();
      }
    }
    catch (JsonException)
    {
      // Ignore parse errors, return empty
    }

    return new List<SearchAttributeDto>();
  }

  private async Task<Result<string>> ExecuteSearchInfoAsync(
      Dictionary<string, object> parameters,
      CancellationToken cancellationToken)
  {
    var keyWord = GetStringValue(parameters["query"]) ?? string.Empty;
    var category = GetStringValue(parameters.GetValueOrDefault("category"));
    var priceMin = GetDoubleValue(parameters.GetValueOrDefault("price_min"));
    var priceMax = GetDoubleValue(parameters.GetValueOrDefault("price_max"));

    try
    {
      // Build enhanced query with filters
      var enhancedQuery = BuildSearchQuery(keyWord, category, priceMin, priceMax);

      logger.LogInformation("Executing Azure AI Search for products with query: {Query}", enhancedQuery);

      var documentSearchResult = await retrievalService.RetrieveContextAsync(
          enhancedQuery,
          new RetrievalOptions
          {
            TopK = _retrievalSettings.TopK,
            MinScore = _retrievalSettings.HybridSearchMinScoreThreshold ?? _retrievalSettings.MinScoreThreshold,
            Strategy = SearchStrategy.Hybrid
          },
          cancellationToken);

      if (documentSearchResult.IsSuccess)
      {
        logger.LogInformation("Successfully searched products with Azure AI Search. Query: {Query}, Results: {ResultCount}",
            enhancedQuery, documentSearchResult.Value.Results?.Count ?? 0);

        return Result.Success($"Product Search Results (Azure AI Search):\n{documentSearchResult.Value.FormattedContext}");
      }

      logger.LogWarning("Azure AI Search returned no results for query: {Query}", enhancedQuery);
      return Result.Error($"No products found matching your search criteria: {keyWord}");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error executing Azure AI Search for products with query: {Query}", keyWord);
      return Result.Error($"Product search failed: {ex.Message}");
    }
  }

  private async Task<Result<string>> ExecuteRemoveFromCartAsync(
    Dictionary<string, object> parameters,
    string userId,
    CancellationToken cancellationToken)
  {
    try
    {
      var productId = GetStringValue(parameters["product_id"])!;
      var productName = GetStringValue(parameters.GetValueOrDefault("product_name")) ?? $"Product #{productId}";
      var quantity = GetIntValue(parameters.GetValueOrDefault("quantity"), 1);

      var requestBody = new
      {
        userId,
        productId,
        quantity
      };

      var json = JsonSerializer.Serialize(requestBody);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      using var request = new HttpRequestMessage(HttpMethod.Post, _shoppingApiConfig.Endpoints.RemoveFromCart)
      {
        Content = content
      };

      AddAuthenticationHeader(request);

      var response = await httpClient.SendAsync(request, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Successfully removed product {ProductId} ({ProductName}) from cart for user {UserId}",
            productId, productName, userId);

        // Build detailed success message with product information
        var successMessage = $"✅ Successfully removed from cart:\n" +
                           $"   • Product: {productName}\n" +
                           $"   • Product ID: {productId}\n" +
                           $"   • Quantity removed: {quantity}";
        return Result.Success(successMessage);
      }

      var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning("Failed to remove product from cart. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
      return Result.Error($"Failed to remove product from cart: {response.StatusCode}");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling remove from cart API");
      return Result.Error($"Remove from cart failed: {ex.Message}");
    }
  }

  #endregion

  private static string BuildSearchQuery(string keyword, string? category, double? priceMin, double? priceMax)
  {
    var queryParts = new List<string> { keyword };

    if (!string.IsNullOrEmpty(category))
    {
      queryParts.Add($"category:{category}");
    }

    if (priceMin.HasValue)
    {
      queryParts.Add($"price_min:{priceMin.Value}");
    }

    if (priceMax.HasValue)
    {
      queryParts.Add($"price_max:{priceMax.Value}");
    }

    return string.Join(" ", queryParts);
  }

  private async Task<Result<string>> ExecuteGetProductInfoAsync(
      Dictionary<string, object> parameters,
      CancellationToken cancellationToken)
  {
    try
    {
      var productId = GetStringValue(parameters["product_id"])!;

      var endpoint = _shoppingApiConfig.Endpoints.GetProductInfo.Replace("{productId}", Uri.EscapeDataString(productId));

      using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
      AddAuthenticationHeader(request);

      var response = await httpClient.SendAsync(request, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Successfully retrieved product info for: {ProductId}", productId);
        return Result.Success(responseContent);
      }

      var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
      logger.LogWarning("Failed to get product info. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
      return Result.Error($"Get product info failed: {response.StatusCode}");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling get product info API");
      return Result.Error($"Get product info failed: {ex.Message}");
    }
  }

  private static string BuildAddToCartSuccessMessage(
      string productName,
      string productId,
      int quantity,
      List<SearchAttributeDto>? searchAttributes = null)
  {
    var message = new System.Text.StringBuilder();
    message.AppendLine($"✅ Successfully added to cart:");
    message.AppendLine($"   • Product: {productName}");
    message.AppendLine($"   • Product ID: {productId}");
    message.AppendLine($"   • Quantity: {quantity}");

    if (searchAttributes is { Count: > 0 })
    {
      foreach (var attr in searchAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Name)))
      {
        message.AppendLine($"   • {attr.Name}: {attr.Value ?? ""}");
      }
    }

    return message.ToString();
  }

  #region -- Helper Methods for Cart Management (e.g., GetCart) can be added here --

  private static string? GetStringValue(object? value)
  {
    return value switch
    {
      JsonElement jsonElement => jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString() : jsonElement.ToString(),
      string str => str,
      null => null,
      _ => value.ToString()
    };
  }

  private static int GetIntValue(object? value, int defaultValue = 0)
  {
    return value switch
    {
      JsonElement jsonElement => jsonElement.ValueKind == JsonValueKind.Number ? jsonElement.GetInt32() :
                                 int.TryParse(jsonElement.ToString(), out var parsed) ? parsed : defaultValue,
      int intValue => intValue,
      string str => int.TryParse(str, out var parsed) ? parsed : defaultValue,
      null => defaultValue,
      _ => int.TryParse(value.ToString(), out var parsed) ? parsed : defaultValue
    };
  }

  private static double? GetDoubleValue(object? value)
  {
    return value switch
    {
      JsonElement jsonElement => jsonElement.ValueKind == JsonValueKind.Number ? jsonElement.GetDouble() :
                                 double.TryParse(jsonElement.ToString(), out var parsed) ? parsed : null,
      double doubleValue => doubleValue,
      int intValue => intValue,
      string str => double.TryParse(str, out var parsed) ? parsed : null,
      null => null,
      _ => double.TryParse(value.ToString(), out var parsed) ? parsed : null
    };
  }

  private static object? CreateExtraDataFromSearchAttributes(List<SearchAttributeDto>? searchAttributes)
  {
    if (searchAttributes == null || searchAttributes.Count == 0)
    {
      return null;
    }

    // OData API expects PascalCase property names (SearchAttributes, Name, Value)
    return new { SearchAttributes = searchAttributes.Select(a => new { Name = a.Name ?? "", Value = a.Value ?? "" }).ToList() };
  }

  private void AddAuthenticationHeader(HttpRequestMessage request)
  {
    if (!string.IsNullOrEmpty(_shoppingApiConfig.PublicKey) && !string.IsNullOrEmpty(_shoppingApiConfig.SecretKey))
    {
      var credentials = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{_shoppingApiConfig.PublicKey}:{_shoppingApiConfig.SecretKey}"));

      request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }
  }

  #endregion
}

