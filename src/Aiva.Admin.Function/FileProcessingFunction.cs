using System.Text.Json;
using Aiva.Admin.Api.Core.FileAggregate;
using Aiva.Admin.Api.UseCases.Files.EmbedFile;
using Aiva.Admin.Api.UseCases.Files.ProcessFile;
using Azure.Messaging.ServiceBus;
using Mediator;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Functions;

/// <summary>
/// Azure Function that processes uploaded files via Service Bus messages.
/// This replaces the FileProcessingBackgroundService polling approach with an event-driven architecture.
/// 
/// Flow:
/// 1. File uploaded via UploadMultipleFilesHandler
/// 2. Message published to "file-processing-queue" Service Bus queue
/// 3. This function triggered to process the file
/// 4. Executes text extraction and embedding via MediatR commands
/// </summary>

public class FileProcessingFunction
{
  private readonly IServiceScopeFactory _scopeFactory;

  public FileProcessingFunction(IServiceScopeFactory scopeFactory)
  {
    _scopeFactory = scopeFactory;
  }

  [Function("ProcessFile")]
  public async Task ProcessFile(
      [ServiceBusTrigger("file-processing-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
      FunctionContext context)
  {
    var logger = context.GetLogger("ProcessFile");

    try
    {
      var fileProcessingMessage = JsonSerializer.Deserialize<FileProcessingMessage>(message.Body.ToString());
      var fileId = FileId.From(fileProcessingMessage!.FileId);

      using var scope = _scopeFactory.CreateScope();
      var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

      logger.LogInformation("Starting file processing for {FileId}", fileId.Value);

      // Step 1: Extract text
      var extractCommand = new ProcessFileCommand(fileId);
      var extractResult = await mediator.Send(extractCommand);

      if (!extractResult.IsSuccess)
      {
        logger.LogWarning("Extraction failed for {FileId}: {Errors}",
            fileId.Value, string.Join(", ", extractResult.Errors));

        // Let the message retry via Service Bus retry policy
        throw new InvalidOperationException($"Text extraction failed: {string.Join(", ", extractResult.Errors)}");
      }

      logger.LogInformation("Extracted {FileId}: {WordCount} words",
          fileId.Value, extractResult.Value.WordCount);

      // Step 2: Embed (only if extraction succeeded)
      var embedCommand = new EmbedFileCommand(fileId);
      var embedResult = await mediator.Send(embedCommand);

      if (embedResult.IsSuccess && embedResult.Value.Success)
      {
        logger.LogInformation("Successfully processed and embedded {FileId}: {ChunkCount} chunks",
            fileId.Value, embedResult.Value.ChunkCount);
      }
      else
      {
        var errorMessage = embedResult.IsSuccess
            ? embedResult.Value.ErrorMessage
            : string.Join(", ", embedResult.Errors);

        logger.LogWarning("Embedding failed for {FileId}: {Error}", fileId.Value, errorMessage);

        // Let the message retry via Service Bus retry policy
        throw new InvalidOperationException($"Embedding failed: {errorMessage}");
      }
    }
    catch (JsonException ex)
    {
      logger.LogError(ex, "Invalid message format received");
      // Don't retry for invalid message format
      return;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing file");
      throw; // Triggers retry mechanism
    }
  }
}

public record FileProcessingMessage(int FileId, DateTime RequestedAt);
