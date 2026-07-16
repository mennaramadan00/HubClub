namespace HubClub.ViewModels
{
    public class AnalysisViewModel
    {
        public List<AnalysisItem> TopProducts { get; set; } = new();
        public List<AnalysisItem> TopCustomers { get; set; } = new();
        public string MostPopularPackageName { get; set; } = "لا يوجد";
        public int MostPopularPackageCount { get; set; }
    }

    public class AnalysisItem
    {
        public string Name { get; set; } = "";
        public decimal Value { get; set; }
    }
}