namespace SanShine.SaaS.Core.Contract.Requests.PriceApproval
{
    public class PriceApprovalItem
    {
        public string? ProcessingStatus { get; set; }
        public decimal OldPrice { get; set; }
        public decimal? NewPrice { get; set; }
        public string? SalesOrganization { get; set; }
        public string? DistributionChannel { get; set; }
        public string? CustomerAreaId { get; set; }
        public string? CustomerAreaName { get; set; }
        public string? DeliveryTakenById { get; set; }
        public string? DeliveryTakenByName { get; set; }
        public string? CityId { get; set; }
        public string? CityName { get; set; }
        public string? Knumh { get; set; }
        public string? SalesOfficeId { get; set; }
        public string? SalesOffice { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? PriceGroupId { get; set; }
        public string? PaymentCondition { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Material { get; set; }
        public string? ExpireStart { get; set; }
        public string? ExpireEnd { get; set; }
    }
}
