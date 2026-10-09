namespace SanShine.SaaS.Core.Contract.Requests.WasteTracking
{
    public class WasteTrackingRequest
    {
        public string? UniqueId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyRepresentative { get; set; }
        public string? CompanyAddressCity { get; set; }
        public string? CompanyAddressDistrict { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyUcb { get; set; }
        public string? WasteId { get; set; }
        public string? WasteName { get; set; }
        public int WasteCount { get; set; }
        public string? WasteCountUnit { get; set; }
        public string? WasteShipment { get; set; }
        public string? WasteDetails { get; set; }
        public string? VerifiedPhoneNumber { get; set; }
        public string? VerifiedMailAddress { get; set; }
        public string? SelectedFabric { get; set; }
    }
}
