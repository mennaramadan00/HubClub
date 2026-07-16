using System;
using System.Collections.Generic;
using HubClub.Models;
using HubClub.Models.Enums;

namespace HubClub.ViewModels
{
    public class CustomerProfileViewModel
    {
        public Customer Customer { get; set; } = null!;
        public UserPackage? ActivePackage { get; set; }
        public Package? ActivePackageDetails { get; set; }

        // 🟢 الإضافة هنا: سجل تاريخ الباقات بالكامل
        public List<UserPackage> PackagesHistory { get; set; } = new List<UserPackage>();

        public List<SessionHistoryViewModel> SessionHistory { get; set; } = new List<SessionHistoryViewModel>();

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
        public List<string> ProductsSummary { get; set; } = new List<string>();
    }
}