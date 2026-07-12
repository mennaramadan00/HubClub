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

        public int QuantityChanged { get; set; } // الكمية بالسالب أو الموجب
        public string MovementType { get; set; } = string.Empty; // نوع الحركة
        public int? SessionId { get; set; } // رقم الجلسة لو مسحوب في جلسة

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}