using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Table("pricing_settings")]
    public class PricingSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PricingSettingId { get; set; }

        [Required(ErrorMessage = "الحد الأدنى للساعات مطلوب")]
        [Column(TypeName = "decimal(6,2)")]
        [Range(0, 9999.99, ErrorMessage = "القيمة يجب أن تكون أكبر من أو تساوي صفر")]
        [Display(Name = "من (ساعة)")]
        public decimal MinNumberOfHours { get; set; }

        [Required(ErrorMessage = "الحد الأقصى للساعات مطلوب")]
        [Column(TypeName = "decimal(6,2)")]
        [Range(0.5, 9999.99, ErrorMessage = "القيمة يجب أن تكون أكبر من صفر")]
        [Display(Name = "إلى (ساعة)")]
        public decimal MaxNumberOfHours { get; set; }

        [Required(ErrorMessage = "السعر مطلوب")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 99999.99, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        [Display(Name = "السعر")]
        public decimal Price { get; set; }
        //  Soft Delete
        [Display(Name = "مفعل")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}