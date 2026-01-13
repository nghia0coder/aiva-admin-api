using FluentValidation;

namespace Aiva.Admin.Api.Web.SystemPrompts.Delete;

public sealed class DeleteSystemPromptValidator : AbstractValidator<DeleteSystemPromptRequest>
{
  public DeleteSystemPromptValidator()
  {
    RuleFor(x => x.Id)
      .GreaterThan(0);
  }
}

