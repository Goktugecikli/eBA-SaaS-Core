using FluentValidation;
using SanShine.SaaS.Core.Contract.Requests.WasteTracking;

namespace SanShine.SaaS.Core.Contract.Validator;

public class WasteTrackingValidator : AbstractValidator<WasteTrackingRequest>
{
    public WasteTrackingValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("CompanyName is required");
        RuleFor(x => x.CompanyRepresentative).NotEmpty().WithMessage("CompanyRepresentative is required");
        RuleFor(x => x.CompanyAddressCity).NotEmpty().WithMessage("CompanyAddressCity is required");
        RuleFor(x => x.CompanyAddressDistrict).NotEmpty().WithMessage("CompanyAddressDistrict is required");
        RuleFor(x => x.CompanyAddress).NotEmpty().WithMessage("CompanyAddress is required");
        RuleFor(x => x.CompanyUcb).NotEmpty().WithMessage("CompanyUcb is required");
        RuleFor(x => x.WasteId).NotEmpty().WithMessage("WasteId is required");
        RuleFor(x => x.WasteCount).GreaterThan(0).WithMessage("WasteCount must be greater than 0");
        RuleFor(x => x.WasteShipment).NotEmpty().WithMessage("WasteShipment is required");
        RuleFor(x => x.SelectedFabric).NotEmpty().WithMessage("SelectedFabric is required");
    }
}
