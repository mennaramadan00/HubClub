using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Table("packages")]
    public class Package
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "اسم الباقة مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
        [Display(Name = "اسم الباقة")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعر مطلوب")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 99999.99, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        [Display(Name = "السعر")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "عدد الساعات مطلوب")]
        [Column(TypeName = "decimal(6,2)")]
        [Range(0.5, 9999.99, ErrorMessage = "عدد الساعات يجب أن يكون أكبر من صفر")]
        [Display(Name = "عدد الساعات")]
        public decimal NumberOfHours { get; set; }

        [Required(ErrorMessage = "مدة الصلاحية مطلوبة")]
        [Range(1, 3650, ErrorMessage = "مدة الصلاحية بالأيام يجب أن تكون بين 1 و 3650")]
        [Display(Name = "مدة الصلاحية (أيام)")]
        public int Period { get; set; }

        [Display(Name = "متاح")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();
    }
}