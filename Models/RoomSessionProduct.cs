using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    public class RoomSessionProduct
    {
        [Key]
        public int RoomSessionProductId { get; set; }

        [Required]
        public int RoomSessionId { get; set; }
        [ForeignKey("RoomSessionId")]
        public RoomSession RoomSession { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPriceAtSale { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
    }
}