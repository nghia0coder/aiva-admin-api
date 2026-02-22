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
            product_name = new { type = "string", description = "The name of the product to add" },
            quantity = new { type = "integer", description = "Quantity to add (default: 1)" },
            size = new { type = "string", description = "Product size if applicable" },
            color = new { type = "string", description = "Product color if applicable" }
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

  private static object? CreateExtraDataIfNeeded(string? size, string? color)
  {
    if (string.IsNullOrEmpty(size) && string.IsNullOrEmpty(color))
    {
      return null;
    }

    var attributes = new Dictionary<string, string>();

    if (!string.IsNullOrEmpty(size))
      attributes["Size"] = size;

    if (!string.IsNullOrEmpty(color))
      attributes["Color"] = color;

    return new { attributes };
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
      var size = GetStringValue(parameters.GetValueOrDefault("size"));
      var color = GetStringValue(parameters.GetValueOrDefault("color"));

      // Convert productId to integer for OData API
      if (!int.TryParse(productId, out var productIdInt))
      {
        logger.LogWarning("Invalid productId format: {ProductId}. Expected integer.", productId);
        return Result.Error($"Invalid productId format: {productId}. Expected integer.");
      }

      // Create OData request body - OData expects string values for JSON
      var requestBody = new
      {
        customerId = "6",
        productId = productIdInt.ToString(),
        quantity = quantity.ToString(),
        shoppingCartType = "1", // 1 for shopping cart, 2 for wishlist
        storeId = "0", // Use current store
        extraData = CreateExtraDataIfNeeded(size, color)
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
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Successfully added product {ProductId} ({ProductName}) to cart for user {UserId}", 
            productId, productName, userId);
        
        // Build detailed success message with product information
        var successMessage = BuildAddToCartSuccessMessage(productName, productId, quantity, size, color);
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
      string? size, 
      string? color)
  {
    var message = new System.Text.StringBuilder();
    message.AppendLine($"✅ Successfully added to cart:");
    message.AppendLine($"   • Product: {productName}");
    message.AppendLine($"   • Product ID: {productId}");
    message.AppendLine($"   • Quantity: {quantity}");
    
    if (!string.IsNullOrWhiteSpace(size))
    {
      message.AppendLine($"   • Size: {size}");
    }
    
    if (!string.IsNullOrWhiteSpace(color))
    {
      message.AppendLine($"   • Color: {color}");
    }
    
    return message.ToString();
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
                          "URL: http://localhost:5000\n" +
                          "Your cart items are ready for purchase!";

      return await Task.FromResult(Result.Success(checkoutMessage));
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error initiating checkout for user {UserId}", userId);
      return Result.Error($"Checkout failed: {ex.Message}");
    }
  }
}
