using FluentValidation;
using SanShine.SaaS.Core.Contract.Requests.Auth;

namespace SanShine.SaaS.Core.Contract.Validator;

public class LoginAuthValidator : AbstractValidator<RestApiUserLoginRequest>
{
    public LoginAuthValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("UserName is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required").MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }

}
