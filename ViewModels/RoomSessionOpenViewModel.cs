using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HubClub.ViewModels
{
    public class RoomSessionOpenViewModel
    {
        [Required(ErrorMessage = "يرجى اختيار الغرفة")]
        [Display(Name = "الغرفة")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار تسعيرة الساعة")]
        [Display(Name = "سعر الساعة")]
        public decimal SelectedHourlyPrice { get; set; }

        public DateTime StartTime { get; set; }

        [Display(Name = "العميل (اختياري)")]
        public int? SelectedCustomerId { get; set; }

        public bool IsNewCustomer { get; set; }

        [Display(Name = "اسم العميل الجديد")]
        public string? NewCustomerName { get; set; }

        [Display(Name = "رقم الموبايل")]
        public string? NewCustomerPhone { get; set; }

        public List<SelectListItem> AvailableRooms { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailablePrices { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AllCustomers { get; set; } = new List<SelectListItem>();
    }
}