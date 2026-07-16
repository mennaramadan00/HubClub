using System;
using System.Collections.Generic;
using HubClub.Models; // تأكدي أن هذا هو الـ namespace الخاص بالـ Models عندك

namespace HubClub.ViewModels
{
    public class DailyReportViewModel
    {
        // اليوم اللي اختاره العميل (وهو نفسه يوم العمل - BusinessDate)
        public DateTime SelectedDate { get; set; }

        // نافذة يوم العمل الفعلية (مفيدة نعرضها للعميل عشان يفهم إيه الداخل في اليوم ده)
        // مثال: لو الكاشير فتح من 8:30 صباحاً يوم 11 لحد 8:30 صباحاً يوم 12
        public DateTime BusinessDayStart { get; set; }
        public DateTime BusinessDayEnd { get; set; }

        public List<Session> Sessions { get; set; } = new();
        public List<ProductReportItem> InventoryReport { get; set; } = new();

        // ملخص الإيرادات
        public decimal TotalRevenue { get; set; }        // إجمالي كل حاجة (وقت + منتجات) للجلسات المغلقة فقط
        public decimal TotalTimeRevenue { get; set; }     // إيراد الوقت فقط
        public decimal TotalProductRevenue { get; set; }  // إيراد المنتجات فقط
        public decimal TotalPackageRevenue
        {
            get; set;
        }
        public int ClosedSessionsCount { get; set; }
        public int OpenSessionsCount { get; set; }        // جلسات لسه مفتوحة (عملاء موجودين دلوقتي)

        public List<PaymentTypeSummaryItem> PaymentBreakdown { get; set; } = new();
    }

    public class ProductReportItem
    {
        public string ProductName { get; set; }
        public int StartQuantity { get; set; }
        public int SoldQuantity { get; set; }    // المبيعات (حركة بيع)
        public int AddedQuantity { get; set; }   // الإضافة (Stock In)
        public int DeficitQuantity { get; set; } // العجز (Deficit)
        public int EndQuantity { get; set; }
    }

    public class PaymentTypeSummaryItem
    {
        public string PaymentTypeName { get; set; }
        public int SessionsCount { get; set; }
        public decimal Revenue { get; set; }
    }
}