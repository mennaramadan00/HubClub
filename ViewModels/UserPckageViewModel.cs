using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HubClub.ViewModels
{
    public class BuyPackageViewModel
    {
        // 1. بيانات العميل (مسجل أو جديد)
        public bool IsNewCustomer { get; set; }
        public int? SelectedCustomerId { get; set; }
        public string? NewCustomerName { get; set; }
        public string? NewCustomerPhone { get; set; }

        // 2. الباقة المختارة
        [Required(ErrorMessage = "يرجى اختيار الباقة المطلوبة")]
        public int SelectedPackageId { get; set; }

        // القوائم المنسدلة
        public List<SelectListItem> AllCustomers { get; set; } = new();
        public List<SelectListItem> AvailablePackages { get; set; } = new();
    }
}