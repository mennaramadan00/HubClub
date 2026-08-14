using System.Collections.Generic;

namespace HubClub.ViewModels
{
    public class AddProductToRoomSessionViewModel
    {
        public int RoomSessionId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }

        // نستخدم نفس الـ Models القديمة الخاصة بأسطر المنتجات لأنها نفس الفكرة
        public List<SessionProductLineViewModel> AlreadyAdded { get; set; } = new List<SessionProductLineViewModel>();
        public List<ProductSelectionItem> AvailableProducts { get; set; } = new List<ProductSelectionItem>();
    }
}