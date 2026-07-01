using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
        [Display(Name = "اسم المنتج")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعر مطلوب")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 99999.99, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        [Display(Name = "السعر")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(0, int.MaxValue, ErrorMessage = "الكمية لا يمكن أن تكون سالبة")]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Display(Name = "متاح")]
        public bool IsActive { get; set; } = true;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<SessionProduct> SessionProducts { get; set; } = new List<SessionProduct>();
    }
}