using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SanShine.Saas.Core.WebApi.Requests;
using SanShine.SaaS.Core.Application.Services;
using SanShine.SaaS.Core.Contract.Requests.PriceApproval;
using SanShine.SaaS.Core.Contract.Requests.WasteTracking;

namespace SanShine.Saas.Core.WebApi.Controllers
{
    [Route("api/rest/")]
    [ApiController]
    [Authorize]
    public class ProcessController : CustomBaseController
    {
        private readonly IPriceApprovalService _priceApprovalService;
        private readonly IWasteTrackingService _wasteTrackingService;

        public ProcessController(IPriceApprovalService priceApprovalService, IWasteTrackingService wasteTrackingService)
        {
            _priceApprovalService = priceApprovalService;
            _wasteTrackingService = wasteTrackingService;
        }

        [HttpPost("start-price-approval")]
        public async Task<IActionResult> StartPriceApproval([FromBody] PriceApprovalRequest request)
        {
            return await HandleServiceResponse(() => _priceApprovalService.StartPriceApprovalAsync(request));
        }

        [HttpPost("start-waste-tracking")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(52_428_800)]
        public async Task<IActionResult> StartWasteTracking([FromForm] WasteTrackingFormRequest request)
        {
            var wasteTrackingRequest = new WasteTrackingRequest
            {
                UniqueId = request.UniqueId,
                CompanyName = request.CompanyName,
                CompanyRepresentative = request.CompanyRepresentative,
                CompanyAddressCity = request.CompanyAddressCity,
                CompanyAddressDistrict = request.CompanyAddressDistrict,
                CompanyAddress = request.CompanyAddress,
                CompanyUcb = request.CompanyUcb,
                WasteId = request.WasteId,
                WasteName = request.WasteName,
                WasteCount = request.WasteCount,
                WasteCountUnit = request.WasteCountUnit,
                WasteShipment = request.WasteShipment,
                WasteDetails = request.WasteDetails,
                VerifiedPhoneNumber = request.VerifiedPhoneNumber,
                VerifiedMailAddress = request.VerifiedMailAddress,
                SelectedFabric = request.SelectedFabric
            };

            var image1 = await ReadFileBytesAsync(request.Image1);
            var image2 = await ReadFileBytesAsync(request.Image2);
            var image3 = await ReadFileBytesAsync(request.Image3);

            return await HandleServiceResponse(() =>
                _wasteTrackingService.StartWasteTrackingAsync(wasteTrackingRequest, image1, image2, image3));
        }

        private static async Task<byte[]?> ReadFileBytesAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
