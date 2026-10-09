using eBAServiceReference;
using Microsoft.Extensions.Configuration;
using SanShine.SaaS.Core.Common.Constants;
using SanShine.SaaS.Core.Common.Core.Utilities;
using SanShine.SaaS.Core.Contract.Responses.BaseResponses;
using WorkflowParameter = eBAServiceReference.WorkflowParameter;

namespace SanShine.SaaS.Core.Contract.Shared
{
    public static class EbaHelper
    {
        private static readonly EbaIntegrationInfo _ebaIntegrationInfo;
        static EbaHelper()
        {
            IConfiguration configuration = Utilities.GetConfiguration();

            _ebaIntegrationInfo = configuration.GetSection("EbaIntegrationInfo").Get<EbaIntegrationInfo>();
        }

        public static async Task<ValidationResult> StartProcess(string processName, Object obj, bool hasDefaultTriggerValue = true, bool triggerValuesHasVKey = true)
        {
            if (obj == null)
                return new ValidationResult
                {
                    Success = false,
                    Message = "Object cannot be empty."
                };
            var response = new ValidationResult();
            var processId = string.Empty;
            var client = new ebawsapiSoapClient(_ebaIntegrationInfo.Domain);
            try
            {
                var workflowParameters = new List<WorkflowParameter>();

                if (hasDefaultTriggerValue)
                {
                    workflowParameters.Add(new WorkflowParameter()
                    {
                        name = "V_IsTriggered",
                        value = "1"
                    });
                }


                foreach (var propertyInfo in obj.GetType().GetProperties())
                {
                    if (!propertyInfo.CanRead)
                        continue;

                    var rawValue = propertyInfo.GetValue(obj);
                    var parameterValue = ToWorkflowParameterValue(rawValue);
                    if (string.IsNullOrEmpty(parameterValue))
                        continue;

                    workflowParameters.Add(new WorkflowParameter
                    {
                        name = triggerValuesHasVKey ? $"V_{propertyInfo.Name}" : propertyInfo.Name,
                        value = parameterValue
                    });
                }
                client.Open();
                var result = await client.StartProcessAsync(_ebaIntegrationInfo.UserId, _ebaIntegrationInfo.Password, _ebaIntegrationInfo.Language, processName, workflowParameters.ToArray());
                processId = result.ProcessId.ToString();
            }
            catch (Exception ex)
            {
                LogHelper.WriteToFileAsync($"[ERROR] Süreç tetiklenirken hata meydana geldi.\n->Hata: {ex}",
                    LogConstants.CommonFileName, LogConstants.FolderName);
                response.Success = false;
                response.Message =
                    $"Süreç tetiklenirken hata meydana geldi. Lütfen Yetkili kişi ile iletişime geçiniz. \n Hata Kısa Metni: {ex.Message}";
                return response;
            }
            finally
            {
                client.Close();
            }

            response.Success = true;
            response.Message = processId;

            return response;
        }

        private static string? ToWorkflowParameterValue(object? rawValue)
        {
            if (rawValue is byte[] bytes)
                return bytes.Length == 0 ? null : Convert.ToBase64String(bytes);

            return rawValue?.ToString();
        }
    }
}
