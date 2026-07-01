namespace HubClub.ViewModels
{
    public class ProductSelectionItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int SelectedQuantity { get; set; } = 0;
        public int AvailableStock { get; set; }  // ← add this
    }
}