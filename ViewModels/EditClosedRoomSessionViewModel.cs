using HubClub.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HubClub.ViewModels
{
    public class EditClosedRoomSessionViewModel
    {
        public int RoomSessionId { get; set; }
        public string RoomName { get; set; } = string.Empty;

        [Display(Name = "العميل")]
        public int? CustomerId { get; set; }

        [Display(Name = "طريقة الدفع")]
        public PaymentMethod? PaymentMethod { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public decimal HoursElapsed { get; set; }

        [Display(Name = "سعر الوقت (قابل للتعديل للمدير)")]
        public decimal TotalTimePrice { get; set; }

        public decimal TotalProductPrice { get; set; }
        public decimal GrandTotal { get; set; }

        public List<SelectListItem> AllCustomers { get; set; } = new List<SelectListItem>();

        // قائمة الطلبات الخاصة بالغرفة
        public List<EditRoomSessionProductViewModel> Products { get; set; } = new List<EditRoomSessionProductViewModel>();
    }

    public class EditRoomSessionProductViewModel
    {
        public int RoomSessionProductId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPriceAtSale { get; set; }
    }
}