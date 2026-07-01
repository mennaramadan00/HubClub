// Customer/Profile — customer info + history tab + active package

using System;
using System.Collections.Generic;
using HubClub.Models; // للوصول لـ PaymentType
using HubClub.Models.Enums;
namespace HubClub.ViewModels
{
    public class CustomerProfileViewModel
    {
        public Customer Customer { get; set; }
        public UserPackage? ActivePackage { get; set; }      // current active package
        public Package? ActivePackageDetails { get; set; }   // package name/hours
        public List<SessionHistoryViewModel> SessionHistory { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalVisits { get; set; }
    }

    public class SessionHistoryViewModel
    {
        public int SessionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal GrandTotal { get; set; }
        public PaymentType PaymentType { get; set; }
        public List<string> ProductsSummary { get; set; }
    }
}