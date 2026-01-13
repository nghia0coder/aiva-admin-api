using FluentValidation;

namespace Aiva.Admin.Api.Web.SystemPrompts.Create;

public sealed class CreateSystemPromptValidator : AbstractValidator<CreateSystemPromptRequest>
{
  public CreateSystemPromptValidator()
  {
    RuleFor(x => x.Key)
      .NotEmpty()
      .MaximumLength(Core.SystemPromptAggregate.SystemPromptKey.MaxLength);

    RuleFor(x => x.Name)
      .NotEmpty()
      .MaximumLength(200);

    RuleFor(x => x.Content)
      .NotEmpty();

    RuleFor(x => x.Category)
      .MaximumLength(100)
      .When(x => x.Category is not null);

    RuleFor(x => x.Description)
      .MaximumLength(1000)
      .When(x => x.Description is not null);
  }
}

