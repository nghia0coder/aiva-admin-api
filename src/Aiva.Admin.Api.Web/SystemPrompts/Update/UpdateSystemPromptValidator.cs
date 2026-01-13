using FluentValidation;

namespace Aiva.Admin.Api.Web.SystemPrompts.Update;

public sealed class UpdateSystemPromptValidator : AbstractValidator<UpdateSystemPromptRequest>
{
  public UpdateSystemPromptValidator()
  {
    RuleFor(x => x.Id)
      .GreaterThan(0);

    RuleFor(x => x.Name)
      .NotEmpty()
      .MaximumLength(200);

    RuleFor(x => x.Content)
      .NotEmpty();

    RuleFor(x => x.Description)
      .MaximumLength(1000)
      .When(x => x.Description is not null);
  }
}

