using FluentValidation;
using SanShine.SaaS.Core.Contract.Requests.Health;

namespace SanShine.SaaS.Core.Contract.Validator;

public class HealthValidator : AbstractValidator<HealthRequest>
{
    public HealthValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Message is required");
    }
}
