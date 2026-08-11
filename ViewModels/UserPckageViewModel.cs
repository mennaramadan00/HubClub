using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using HubClub.Models.Enums;

namespace HubClub.ViewModels
{
    public class BuyPackageViewModel
    {
        // 1. بيانات العميل (مسجل أو جديد)
        public bool IsNewCustomer { get; set; }
        public int? SelectedCustomerId { get; set; }
        public string? NewCustomerName { get; set; }
        public string? NewCustomerPhone { get; set; }

        [Required(ErrorMessage = "يرجى اختيار طريقة الدفع")]
        [Display(Name = "طريقة التحصيل")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        // 2. الباقة المختارة
        [Required(ErrorMessage = "يرجى اختيار الباقة المطلوبة")]
        public int SelectedPackageId { get; set; }

        // القوائم المنسدلة
        public List<SelectListItem> AllCustomers { get; set; } = new();
        public List<SelectListItem> AvailablePackages { get; set; } = new();
    }
}