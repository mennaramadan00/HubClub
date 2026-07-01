// Customer/Index — search bar + results list

using System;
using System.Collections.Generic;
using HubClub.Models; // للوصول لـ PaymentType
using HubClub.Models.Enums;
namespace HubClub.ViewModels
{
    public class CustomerSearchViewModel
    {
        public string? SearchQuery { get; set; }
        public List<Customer> Results { get; set; } = new();
        public bool HasSearched { get; set; } = false;
    }
}