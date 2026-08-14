using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HubClub.Models.Enums;

namespace HubClub.Models
{
    public class RoomSession
    {
        [Key]
        public int RoomSessionId { get; set; }

        [Required]
        public int RoomId { get; set; }
        [ForeignKey("RoomId")]
        public Room Room { get; set; } = null!;

        // 🟢 العميل هنا أصبح اختياري (Nullable)
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTimePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalProductPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        public bool IsClosed { get; set; }

        public DateOnly BusinessDate { get; set; }

        public ICollection<RoomSessionProduct> RoomSessionProducts { get; set; } = new List<RoomSessionProduct>();
    }
}