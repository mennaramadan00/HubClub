using HubClub.Models;

namespace HubClub.ViewModels
{
    public class SessionDetailsViewModel
    {
        public Session Session { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public decimal HoursElapsed { get; set; }
        public UserPackage? UserPackage { get; set; }
        public PricingSetting? PricingSetting { get; set; }
        public List<SessionProductLineViewModel> Products { get; set; } = new();
    }

    public class SessionProductLineViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}