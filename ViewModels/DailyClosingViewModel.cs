// Closing/Index — daily cash summary
using System;
using System.Collections.Generic;
using HubClub.Models; // للوصول لـ PaymentType
using HubClub.Models.Enums;
namespace HubClub.ViewModels
{
    public class DailyClosingViewModel
    {
        public DateOnly BusinessDate { get; set; }
        public decimal TotalTimeRevenue { get; set; }
        public decimal TotalProductRevenue { get; set; }
        public decimal GrandTotal { get; set; }
        public List<ProductSalesSummary> ProductBreakdown { get; set; }
        public int TotalSessionsCount { get; set; }
        public int PackageSessionsCount { get; set; }
        public int NormalSessionsCount { get; set; }
        public bool AlreadyClosed { get; set; }
    }

    public class ProductSalesSummary
    {
        public string ProductName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}