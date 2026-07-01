using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Table("session_products")]
    public class SessionProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SProductId { get; set; }

        // Foreign Keys
        [Required]
        [ForeignKey("Session")]
        public int SessionId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 99999.99, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        [Display(Name = "سعر الوحدة وقت البيع")]
        public decimal UnitPriceAtSale { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الإجمالي")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public Session Session { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}