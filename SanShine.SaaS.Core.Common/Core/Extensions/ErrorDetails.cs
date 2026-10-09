using Newtonsoft.Json;

namespace SanShine.SaaS.Core.Common.Core.Extensions
{
    public class ErrorDetails
    {
        public string Message { get; set; }
        public string ErrorId { get; set; }
        public int StatusCode { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
