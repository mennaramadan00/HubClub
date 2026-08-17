using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HubClub.ViewModels
{
    public class IncomeReportViewModel
    {
        [Display(Name = "من تاريخ")]
        public DateOnly StartDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        public DateOnly EndDate { get; set; }

        // تفصيل الإيرادات
        public decimal SessionTimeRevenue { get; set; }
        public decimal SessionBarRevenue { get; set; }
        public decimal RoomTimeRevenue { get; set; }
        public decimal RoomBarRevenue { get; set; }
        public decimal PackagesRevenue { get; set; }

        // الإجمالي
        public decimal TotalRevenue =>
            SessionTimeRevenue + SessionBarRevenue + RoomTimeRevenue + RoomBarRevenue + PackagesRevenue;

        // المصروفات والمشتريات
        public decimal TotalExpenses { get; set; }

        // صافي الدخل
        public decimal NetIncome => TotalRevenue - TotalExpenses;

        // 🟢 بيانات الرسم البياني الخطي (الإيرادات اليومية)
        public List<string> DailyLabels { get; set; } = new List<string>();
        public List<decimal> DailyRevenues { get; set; } = new List<decimal>();
    }
}