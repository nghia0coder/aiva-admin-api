using System.ClientModel;
using Ardalis.Result;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Embeddings;

namespace Aiva.Admin.Api.Infrastructure.Embedding;

using Core.Interfaces;

/// <summary>
/// Azure OpenAI implementation of embedding generation
/// </summary>
public sealed class AzureOpenAIEmbeddingService : IEmbeddingService
{
  private readonly EmbeddingClient _embeddingClient;
  private readonly EmbeddingConfiguration _configuration;
  private readonly ILogger<AzureOpenAIEmbeddingService> _logger;

  public AzureOpenAIEmbeddingService(
      IOptions<EmbeddingConfiguration> options,
      ILogger<AzureOpenAIEmbeddingService> logger)
  {
    _configuration = options.Value;
    _logger = logger;
    _embeddingClient = CreateEmbeddingClient();
  }

  public int EmbeddingDimension => _configuration.Dimension;
  public string ModelName => _configuration.DeploymentName;

  private EmbeddingClient CreateEmbeddingClient()
  {
    AzureOpenAIClient azureClient;

    if (_configuration.UseManagedIdentity)
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_configuration.Endpoint),
          new DefaultAzureCredential());
    }
    else if (!string.IsNullOrEmpty(_configuration.ApiKey))
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_configuration.Endpoint),
          new AzureKeyCredential(_configuration.ApiKey));
    }
    else
    {
      throw new InvalidOperationException(
          "Embedding configuration is invalid. " +
          "Provide either ApiKey or enable UseManagedIdentity.");
    }

    return azureClient.GetEmbeddingClient(_configuration.DeploymentName);
  }

  public async Task<Result<ReadOnlyMemory<float>>> GenerateEmbeddingAsync(
      string text,
      CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        return Result.Invalid(new ValidationError("text", "Text cannot be empty"));
      }

      var response = await _embeddingClient.GenerateEmbeddingAsync(
          text,
          cancellationToken: cancellationToken);

      var embedding = response.Value.ToFloats();

      _logger.LogDebug(
          "Generated embedding for text of length {Length}, dimension {Dimension}",
          text.Length, embedding.Length);

      return Result.Success(embedding);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during embedding generation");
      return Result.Error($"Embedding generation failed: {ex.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during embedding generation");
      return Result.Error($"Unexpected error: {ex.Message}");
    }
  }

  public async Task<Result<IReadOnlyList<ReadOnlyMemory<float>>>> GenerateEmbeddingsAsync(
      IReadOnlyList<string> texts,
      CancellationToken cancellationToken = default)
  {
    try
    {
      if (texts.Count == 0)
      {
        return Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>([]);
      }

      var results = new List<ReadOnlyMemory<float>>(texts.Count);

      // Process in batches
      for (var i = 0; i < texts.Count; i += _configuration.MaxBatchSize)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var batch = texts
            .Skip(i)
            .Take(_configuration.MaxBatchSize)
            .ToList();

        var response = await _embeddingClient.GenerateEmbeddingsAsync(
            batch,
            cancellationToken: cancellationToken);

        foreach (var embedding in response.Value)
        {
          results.Add(embedding.ToFloats());
        }

        _logger.LogDebug(
            "Generated {Count} embeddings in batch {BatchIndex}",
            batch.Count, i / _configuration.MaxBatchSize + 1);
      }

      return Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during batch embedding generation");
      return Result.Error($"Batch embedding generation failed: {ex.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during batch embedding generation");
      return Result.Error($"Unexpected error: {ex.Message}");
    }
  }
}
