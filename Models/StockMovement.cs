using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // قمنا بتغيير الاسم هنا لـ Timestamp ليتوافق مع كود التقرير
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}