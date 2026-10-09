using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SanShine.SaaS.Core.Application.Shared;
using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Common.Core.Aspects.Autofac.Validation;
using SanShine.SaaS.Core.Common.Core.Utilities;
using SanShine.SaaS.Core.Contract.Requests.PriceApproval;
using SanShine.SaaS.Core.Contract.Responses;
using SanShine.SaaS.Core.Contract.Shared;
using SanShine.SaaS.Core.Contract.Validator;

namespace SanShine.SaaS.Core.Application.Services
{
    public interface IPriceApprovalService
    {
        Task<ProcessOperationResponse> StartPriceApprovalAsync(PriceApprovalRequest request);
    }

    public class PriceApprovalService : IPriceApprovalService
    {
        [ValidationAspect(typeof(PriceApprovalValidator))]
        public async Task<ProcessOperationResponse> StartPriceApprovalAsync(PriceApprovalRequest request)
        {
            await LogHelper.WriteToFileAsync(
                $"Süreç Tetiklendi.\nProcessName: CimkoHazirBeton\nGelen veriler kontrol ediliyor...",
                LogConstants.CommonFileName,
                LogConstants.FolderName);

            var response = new ProcessOperationResponse();

            var apiParameterData = JsonConvert.SerializeObject(
                new
                {
                    conditionType = request.ConditionType,
                    companyCode = request.CompanyCode,
                    tableName = request.TableName,
                    items = request.Items
                },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var processResult = await EbaHelper.StartProcess(
                "CimkoHazirBeton",
                new
                {
                    v_apiParameterData = apiParameterData,
                    v_flowStarterUserId = request.FlowStarterUserId,
                    v_isTrigger = "1"
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
                $"Süreç başladı. Süreç No:{response.ProcessId}",
                LogConstants.CommonFileName,
                LogConstants.FolderName);

            return response;
        }
    }
}
