using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SanShine.SaaS.Core.Application.Shared;
using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Common.Core.Aspects.Autofac.Validation;
using SanShine.SaaS.Core.Common.Core.Utilities;
using SanShine.SaaS.Core.Contract.Requests.WasteTracking;
using SanShine.SaaS.Core.Contract.Responses;
using SanShine.SaaS.Core.Contract.Shared;
using SanShine.SaaS.Core.Contract.Validator;

namespace SanShine.SaaS.Core.Application.Services
{
    public interface IWasteTrackingService
    {
        Task<ProcessOperationResponse> StartWasteTrackingAsync(
            WasteTrackingRequest request,
            byte[]? image1ByteArray,
            byte[]? image2ByteArray,
            byte[]? image3ByteArray);
    }

    public class WasteTrackingService : IWasteTrackingService
    {
        [ValidationAspect(typeof(WasteTrackingValidator))]
        public async Task<ProcessOperationResponse> StartWasteTrackingAsync(
            WasteTrackingRequest request,
            byte[]? image1ByteArray,
            byte[]? image2ByteArray,
            byte[]? image3ByteArray)
        {
            await LogHelper.WriteToFileAsync(
                $"Süreç Tetiklendi.\nProcessName: WasteTracking\nGelen veriler kontrol ediliyor...",
                LogConstants.CommonFileName,
                LogConstants.FolderName);

            var response = new ProcessOperationResponse();
            var uniqueId = string.IsNullOrWhiteSpace(request.UniqueId)
                ? Guid.NewGuid().ToString("N")
                : request.UniqueId;

            var apiParameterData = JsonConvert.SerializeObject(
                new
                {
                    uniqueId,
                    companyName = request.CompanyName,
                    companyRepresentative = request.CompanyRepresentative,
                    companyAddressCity = request.CompanyAddressCity,
                    companyAddressDistrict = request.CompanyAddressDistrict,
                    companyAddress = request.CompanyAddress,
                    companyUCB = request.CompanyUcb,
                    wasteId = request.WasteId,
                    wasteName = request.WasteName,
                    wasteCount = request.WasteCount,
                    wasteCountUnit = request.WasteCountUnit,
                    wasteShipment = request.WasteShipment,
                    wasteDetails = request.WasteDetails,
                    verifiedPhoneNumber = request.VerifiedPhoneNumber,
                    verifiedMailAddress = request.VerifiedMailAddress,
                    selectedFabric = request.SelectedFabric
                },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var processResult = await EbaHelper.StartProcess(
                "WasteTracking",
                new
                {
                    v_apiParameterData = apiParameterData,
                    v_isTrigger = "1",
                    v_image1ByteArray = image1ByteArray,
                    v_image2ByteArray = image2ByteArray,
                    v_image3ByteArray = image3ByteArray
                },
                hasDefaultTriggerValue: false,
                triggerValuesHasVKey: false);

            if (!processResult.Success)
            {
                response.Message = processResult.Message ?? ResponseMessages.ProcessNotStarted;
                return response;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.ProcessId = processResult.Message;
            response.Message = ResponseMessages.Success;

            await LogHelper.WriteToFileAsync(
                $"Süreç başladı. Süreç No:{response.ProcessId} UniqueId:{uniqueId}",
                LogConstants.CommonFileName,
                LogConstants.FolderName);

            return response;
        }
    }
}
