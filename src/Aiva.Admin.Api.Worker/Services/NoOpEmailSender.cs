using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Worker.Services;

/// <summary>
/// No-op implementation for Worker project.
/// Worker doesn't send emails.
/// </summary>
public class NoOpEmailSender : IEmailSender
{
  private readonly ILogger<NoOpEmailSender> _logger;

  public NoOpEmailSender(ILogger<NoOpEmailSender> logger)
  {
    _logger = logger;
  }

  public Task SendEmailAsync(string to, string from, string subject, string body)
  {
    _logger.LogDebug(
        "No-op: Skipping email send to {To} with subject {Subject} (Worker project)",
        to, subject);
    return Task.CompletedTask;
  }
}
