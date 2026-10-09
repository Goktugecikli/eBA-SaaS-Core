namespace SanShine.SaaS.Core.Contract.Requests.PriceApproval
{
    public class PriceApprovalRequest
    {
        public string? FlowStarterUserId { get; set; }
        public string? ConditionType { get; set; }
        public string? CompanyCode { get; set; }
        public string? TableName { get; set; }
        public List<PriceApprovalItem>? Items { get; set; }
    }
}
