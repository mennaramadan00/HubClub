using System;
using System.Collections.Generic;
using HubClub.Models;
using HubClub.Models.Enums;

namespace HubClub.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<SessionCardViewModel> ActiveSessions { get; set; } = new List<SessionCardViewModel>();
        public List<SessionCardViewModel> ClosedSessions { get; set; } = new List<SessionCardViewModel>();

        public List<RoomSessionCardViewModel> ActiveRoomSessions { get; set; } = new List<RoomSessionCardViewModel>();
        public List<RoomSessionCardViewModel> ClosedRoomSessions { get; set; } = new List<RoomSessionCardViewModel>();

        public DateOnly BusinessDate { get; set; }
        public int ActiveCustomersCount { get; set; }

        public decimal TodayTotalTimeCash { get; set; }
        public decimal TodayTotalProductCash { get; set; }
        public decimal TodayTotalPackageCash { get; set; }

        // 🟢 خاصية جديدة لإيرادات الغرف
        public decimal TodayTotalRoomCash { get; set; }

        public decimal TodayTotalCash { get; set; }
        public decimal TodayTotalCashMethod { get; set; }
        public decimal TodayTotalInstaPayMethod { get; set; }
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
        public bool HasPackage { get; set; }
        public decimal TotalTimePrice { get; set; }
        public decimal TotalProductPrice { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TodayTotalPackageCash { get; set; }
        public List<string> ProductNames { get; set; } = new List<string>();
    }

    public class RoomSessionCardViewModel
    {
        public int RoomSessionId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }

        // 🟢 تمت إضافة خصائص النهاية والإجمالي للغرفة
        public DateTime? EndTime { get; set; }
        public decimal GrandTotal { get; set; }

        public List<string> ProductNames { get; set; } = new List<string>();
    }
}