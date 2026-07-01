using Microsoft.AspNetCore.Mvc.Rendering;
using HubClub.Models.Enums;

namespace HubClub.ViewModels
{
    public class SessionOpenViewModel
    {
        public int? SelectedCustomerId { get; set; }
        public List<SelectListItem> AllCustomers { get; set; } = new();
        public bool IsNewCustomer { get; set; } = false;
        public string? NewCustomerName { get; set; }
        public string? NewCustomerPhone { get; set; }
        public PaymentType PaymentType { get; set; } = PaymentType.PerSession;
        public DateTime StartTime { get; set; } = DateTime.Now;
    }
}