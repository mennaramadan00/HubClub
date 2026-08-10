using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using HubClub.Models.Enums;

namespace HubClub.ViewModels
{
    // الكلاس الجديد لتمثيل كل منتج داخل شاشة التعديل
    public class EditSessionProductViewModel
    {
        public int SessionProductId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "الكمية")]
        [Range(0, 9999, ErrorMessage = "الكمية غير صالحة")]
        public int Quantity { get; set; }

        [Display(Name = "السعر")]
        public decimal UnitPriceAtSale { get; set; }
    }

    public class EditClosedSessionViewModel
    {
        public int SessionId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار العميل")]
        [Display(Name = "العميل")]
        public int CusId { get; set; }
        public List<SelectListItem>? AllCustomers { get; set; }

        [Required]
        [Display(Name = "نوع الدفع")]
        public PaymentType PaymentType { get; set; }

        [Display(Name = "وقت البداية")]
        public DateTime StartTime { get; set; }

        [Display(Name = "وقت النهاية")]
        public DateTime? EndTime { get; set; }

        [Display(Name = "مدة الجلسة (ساعات)")]
        public decimal HoursElapsed { get; set; }

        [Required]
        [Display(Name = "تكلفة الوقت")]
        public decimal TotalTimePrice { get; set; }

        [Required]
        [Display(Name = "تكلفة المنتجات")]
        public decimal TotalProductPrice { get; set; }

        [Required]
        [Display(Name = "الإجمالي النهائي")]
        public decimal GrandTotal { get; set; }

        // 🟢 القائمة الجديدة التي ستحمل المنتجات للتعديل
        public List<EditSessionProductViewModel> Products { get; set; } = new List<EditSessionProductViewModel>();
    }
}