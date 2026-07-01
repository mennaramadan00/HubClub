using HubClub.Models.Enums;

namespace HubClub.ViewModels
{
    public class SessionCloseViewModel
    {
        public int SessionId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public PaymentType PaymentType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal HoursElapsed { get; set; }
        public string? PricingRangeLabel { get; set; }
        public decimal CalculatedTimePrice { get; set; }
        public bool IsPackageSession { get; set; }
        public List<ProductSelectionItem> AvailableProducts { get; set; } = new();
        public decimal TotalProductPrice { get; set; }
        public decimal GrandTotal { get; set; }
    }
}