namespace Aiva.Admin.Api.Core.Interfaces;

public interface IServiceBusPublisher
{
  Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
      where T : class;
}
