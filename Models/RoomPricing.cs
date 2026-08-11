using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    public class RoomPricing
    {
        [Key]
        public int RoomPricingId { get; set; }

        [Required(ErrorMessage = "سعر الساعة مطلوب")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر الساعة (بالجنيه)")]
        public decimal PricePerHour { get; set; }
    }
}