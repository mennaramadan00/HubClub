// Session/AddProduct — add bar items to active session
using System;
using System.Collections.Generic;
using HubClub.Models; // للوصول لـ PaymentType
using HubClub.Models.Enums;
namespace HubClub.ViewModels
{
    public class AddProductToSessionViewModel
    {
        public int SessionId { get; set; }
        public string CustomerName { get; set; }
        public List<ProductSelectionItem> AvailableProducts { get; set; } = new();
        public List<SessionProductLineViewModel> AlreadyAdded { get; set; } = new();
    }

    
}