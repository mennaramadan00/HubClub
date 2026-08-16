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

        // 🟢 جلسات الصالة المفتوحة والمغلقة
        public List<Session> Sessions { get; set; } = new();

        // 🟢 جلسات الغرف (VIP) - (الإضافة الجديدة)
        public List<RoomSession> RoomSessions { get; set; } = new();

        public List<ProductReportItem> InventoryReport { get; set; } = new();

        // ملخص الإيرادات
        public decimal TotalRevenue { get; set; }        // إجمالي كل حاجة (وقت + منتجات + باقات + غرف)
        public decimal TotalTimeRevenue { get; set; }      // إيراد وقت الصالة
        public decimal TotalProductRevenue { get; set; }   // إيراد المنتجات
        public decimal TotalPackageRevenue { get; set; }   // إيراد الباقات

        // 🟢 إيراد الغرف - (الإضافة الجديدة)
        public decimal TotalRoomRevenue { get; set; }

        public decimal TotalCashMethod { get; set; }
        public decimal TotalInstaPayMethod { get; set; }

        // إحصائيات جلسات الصالة
        public int ClosedSessionsCount { get; set; }
        public int OpenSessionsCount { get; set; }        // جلسات لسه مفتوحة (عملاء موجودين دلوقتي)

        // 🟢 إحصائيات جلسات الغرف - (الإضافة الجديدة)
        public int ClosedRoomSessionsCount { get; set; }
        public int OpenRoomSessionsCount { get; set; }

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