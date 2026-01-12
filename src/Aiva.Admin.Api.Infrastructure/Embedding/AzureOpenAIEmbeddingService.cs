using System.ClientModel;
using Ardalis.Result;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Embeddings;

namespace Aiva.Admin.Api.Infrastructure.Embedding;

using Aiva.Admin.Api.Infrastructure.Configuration;
using Core.Interfaces;

/// <summary>
/// Azure OpenAI implementation of embedding generation
/// </summary>
public sealed class AzureOpenAIEmbeddingService : IEmbeddingService
{
  private readonly EmbeddingClient _embeddingClient;
  private readonly EmbeddingConfiguration _configuration;
  private readonly ILogger<AzureOpenAIEmbeddingService> _logger;
  private readonly AppSettings _appSettings;

  public AzureOpenAIEmbeddingService(
      IOptions<EmbeddingConfiguration> options,
      ILogger<AzureOpenAIEmbeddingService> logger,
      AppSettings appSettings)
  {
    _configuration = options.Value;
    _logger = logger;
    _appSettings = appSettings;
    _embeddingClient = CreateEmbeddingClient();
  }

  public int EmbeddingDimension => _appSettings.Embedding.Dimension;
  public string ModelName => _appSettings.Embedding.DeploymentName;

  private EmbeddingClient CreateEmbeddingClient()
  {
    AzureOpenAIClient azureClient;

    if (_appSettings.Embedding.UseManagedIdentity)
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_appSettings.Embedding.Endpoint),
          new DefaultAzureCredential());
    }
    else if (!string.IsNullOrEmpty(_appSettings.Embedding.ApiKey))
    {

      azureClient = new AzureOpenAIClient(
          new Uri(_appSettings.AzureAI.Endpoint),
          new AzureKeyCredential(_appSettings.AzureAI.ApiKey));
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
      for (var i = 0; i < texts.Count; i += _appSettings.Embedding.MaxBatchSize)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var batch = texts
            .Skip(i)
            .Take(_appSettings.Embedding.MaxBatchSize)
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
            batch.Count, i / _appSettings.Embedding.MaxBatchSize + 1);
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
