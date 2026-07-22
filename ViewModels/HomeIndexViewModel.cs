using System;
using System.Collections.Generic;
using HubClub.Models; // للوصول لـ PaymentType
using HubClub.Models.Enums;
namespace HubClub.ViewModels
{
    // Home/Index — today's dashboard and session cards
    public class HomeIndexViewModel
    {
        // قوائم الجلسات (الكروت)
        public List<SessionCardViewModel> ActiveSessions { get; set; } = new List<SessionCardViewModel>();
        public List<SessionCardViewModel> ClosedSessions { get; set; } = new List<SessionCardViewModel>();

        // بيانات اليوم الأساسية
        public DateOnly BusinessDate { get; set; }
        public int ActiveCustomersCount { get; set; } 

        // ملخص إيرادات اليوم
        public decimal TodayTotalTimeCash { get; set; }   
        public decimal TodayTotalProductCash { get; set; } 
        public decimal TodayTotalPackageCash { get; set; }
        public decimal TodayTotalCash { get; set; }
    }

    public class SessionCardViewModel
    {
        public int SessionId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsClosed { get; set; }

        public PaymentType PaymentType { get; set; }
        public bool HasPackage { get; set; } // الخاصية اللي كانت ناقصة

        public decimal TotalTimePrice { get; set; }
        public decimal TotalProductPrice { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TodayTotalPackageCash { get; set; }
        public List<string> ProductNames { get; set; } = new List<string>();
    }
}