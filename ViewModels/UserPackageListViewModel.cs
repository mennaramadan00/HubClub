namespace HubClub.ViewModels
{
    public class UserPackageListViewModel
    {
        public int Id { get; set; } // أو UserPackageId حسب ما سميتيه في الموديل
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal RemainingHours { get; set; }
    }
}