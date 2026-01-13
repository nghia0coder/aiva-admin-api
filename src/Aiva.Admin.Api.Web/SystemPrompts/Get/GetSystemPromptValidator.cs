using FluentValidation;

namespace Aiva.Admin.Api.Web.SystemPrompts.Get;

public sealed class GetSystemPromptValidator : AbstractValidator<GetSystemPromptRequest>
{
  public GetSystemPromptValidator()
  {
    RuleFor(x => x.Id)
      .GreaterThan(0);
  }
}

