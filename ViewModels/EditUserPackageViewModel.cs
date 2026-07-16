using System;

namespace HubClub.ViewModels
{
    public class EditUserPackageViewModel
    {
        public int UserPackageId { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string PackageName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal Price { get; set; }

        public decimal RemainingHours { get; set; }
    }
}