using FluentValidation;
using SanShine.SaaS.Core.Contract.Requests.Auth;
        
namespace SanShine.SaaS.Core.Contract.Validator;

public class CreateApiUserValidator : AbstractValidator<CreateRestApiUserRequest>
{
    public CreateApiUserValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            //.Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{8,}$")
            .Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{8,}$")
            .WithMessage("Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.");

        RuleFor(x=>x.Company)
            .NotEmpty()
            .WithMessage("Company is required.");
    }
}
