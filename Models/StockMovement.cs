using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HubClub.Helpers;

namespace HubClub.Models
{
    public class StockMovement
    {
        [Key]
        public int StockMovementId { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        public int QuantityChanged { get; set; }
        public string MovementType { get; set; } = string.Empty;

        public int? SessionId { get; set; }
        // 🟢 اللمسة المضافة: Navigation Property للجلسة العادية
        [ForeignKey("SessionId")]
        public Session? Session { get; set; }

        public int? RoomSessionId { get; set; }
        [ForeignKey("RoomSessionId")]
        public RoomSession? RoomSession { get; set; }

        // قمنا بتغيير الاسم هنا لـ Timestamp ليتوافق مع كود التقرير
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateOnly BusinessDate { get; set; } = BusinessHelper.GetBusinessDate(DateTime.Now);
    }
}