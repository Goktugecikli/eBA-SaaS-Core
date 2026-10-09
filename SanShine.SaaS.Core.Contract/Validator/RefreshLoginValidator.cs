using FluentValidation;
using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Common.Core.Utilities;
using SanShine.SaaS.Core.Contract.Requests.Auth;

namespace SanShine.SaaS.Core.Contract.Validator;

public class RefreshLoginValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshLoginValidator()
    {
        RuleFor(l => l.RefreshToken).NotEmpty().WithMessage("Refresh Token cannot be empty");
    }
}
