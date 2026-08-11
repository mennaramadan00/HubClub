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

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // 🟢 سعر الساعة الذي سيختاره الكاشير لهذه الجلسة تحديداً
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTimePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalProductPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        // Cash or InstaPay (No Packages for rooms)
        public PaymentMethod? PaymentMethod { get; set; }

        public bool IsClosed { get; set; }

        public DateOnly BusinessDate { get; set; }

        // علاقة مع المنتجات التي تم طلبها داخل هذه الغرفة
        public ICollection<RoomSessionProduct> RoomSessionProducts { get; set; } = new List<RoomSessionProduct>();
    }
}