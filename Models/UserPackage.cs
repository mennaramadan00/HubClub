using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HubClub.Models.Enums;

namespace HubClub.Models
{
   

    [Table("user_packages")]
    public class UserPackage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserPackageId { get; set; }

        // Foreign Keys
        [Required]
        [ForeignKey("Customer")]
        public int CusId { get; set; }

        [Required]
        [ForeignKey("Package")]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [Display(Name = "تاريخ البداية")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
        [Display(Name = "تاريخ الانتهاء")]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(6,2)")]
        [Range(0, 9999.99, ErrorMessage = "الساعات المتبقية لا يمكن أن تكون سالبة")]
        [Display(Name = "الساعات المتبقية")]
        public decimal RemainingHours { get; set; }

        [Required]
        [Display(Name = "الحالة")]
        public UserPackageStatus Status { get; set; } = UserPackageStatus.Active;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "سعر الشراء")]
        public decimal Price { get; set; }

        [Timestamp]
        public DateTime RowVersion { get; set; }

        // Navigation properties
        public Customer Customer { get; set; } = null!;
        public Package Package { get; set; } = null!;
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}