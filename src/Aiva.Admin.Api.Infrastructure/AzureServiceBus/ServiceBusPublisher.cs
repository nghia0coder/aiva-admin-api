using System.Text.Json;
using Aiva.Admin.Api.Core.Interfaces;
using Azure.Messaging.ServiceBus;

namespace Aiva.Admin.Api.Infrastructure.Messaging;

public class ServiceBusPublisher : IServiceBusPublisher
{
  private readonly ServiceBusClient _client;
  private readonly ILogger<ServiceBusPublisher> _logger;

  public ServiceBusPublisher(ServiceBusClient client, ILogger<ServiceBusPublisher> logger)
  {
    _client = client;
    _logger = logger;
  }

  public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
      where T : class
  {
    await using var sender = _client.CreateSender(queueName);

    try
    {
      var messageBody = JsonSerializer.Serialize(message);
      var serviceBusMessage = new ServiceBusMessage(messageBody)
      {
        ContentType = "application/json"
      };

      await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

      _logger.LogInformation(
          "Successfully published message to queue {QueueName}: {MessageType}",
          queueName,
          typeof(T).Name);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex,
          "Failed to publish message to queue {QueueName}: {MessageType}",
          queueName,
          typeof(T).Name);
      throw;
    }
  }
}
