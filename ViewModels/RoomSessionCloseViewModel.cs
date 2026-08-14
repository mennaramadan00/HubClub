using HubClub.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HubClub.ViewModels
{
    public class RoomSessionCloseViewModel
    {
        public int RoomSessionId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal HoursElapsed { get; set; }

        // 🟢 أضفنا هذا الحقل لتجنب استخدام الـ ViewBag تماماً
        public string ActualDurationText { get; set; } = string.Empty;

        public decimal HourlyPrice { get; set; }
        public decimal CalculatedTimePrice { get; set; }

        public decimal TotalProductPrice { get; set; }

        [Required(ErrorMessage = "طريقة الدفع مطلوبة")]
        public PaymentMethod PaymentMethod { get; set; }

        public List<SessionProductLineViewModel> AlreadyAddedProducts { get; set; } = new List<SessionProductLineViewModel>();
        public List<ProductSelectionItem> AvailableProducts { get; set; } = new List<ProductSelectionItem>();
    }
}