namespace SanShine.SaaS.Core.Contract.Responses
{
    public class HealthResponse : BaseControllerResponse
    {
        public string Echo { get; set; }
        public bool DatabaseReachable { get; set; }
    }
}
