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

    //public class ProductSelectionItem
    //{
    //    public int ProductId { get; set; }
    //    public string Name { get; set; }
    //    public decimal Price { get; set; }
    //    public int SelectedQuantity { get; set; } = 0;
    //    public int AvailableStock { get; set; };
    //}
}