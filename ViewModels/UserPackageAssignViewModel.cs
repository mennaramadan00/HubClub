// UserPackage/Assign — assign a package to a customer
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using HubClub.Models; // للوصول لـ PaymentType
using HubClub.Models.Enums;
namespace HubClub.ViewModels
{
    public class UserPackageAssignViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int SelectedPackageId { get; set; }
        public List<SelectListItem> AvailablePackages { get; set; } = new();
        public decimal PurchasePrice { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
    }
}