using FluentValidation;
using SanShine.SaaS.Core.Contract.Requests.PriceApproval;

namespace SanShine.SaaS.Core.Contract.Validator;

public class PriceApprovalValidator : AbstractValidator<PriceApprovalRequest>
{
    public PriceApprovalValidator()
    {
        RuleFor(x => x.FlowStarterUserId).NotEmpty().WithMessage("FlowStarterUserId is required");
        RuleFor(x => x.ConditionType).NotEmpty().WithMessage("ConditionType is required");
        RuleFor(x => x.CompanyCode).NotEmpty().WithMessage("CompanyCode is required");
        RuleFor(x => x.TableName).NotEmpty().WithMessage("TableName is required");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Items is required");
    }
}
