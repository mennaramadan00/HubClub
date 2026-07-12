using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Table("customers")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [StringLength(11, ErrorMessage = "رقم الهاتف لا يتجاوز 11 رقم")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "رقم الهاتف يجب أن يحتوي على أرقام فقط")]
        [Display(Name = "رقم الهاتف")]
        
        public string Phone { get; set; } = string.Empty;

        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "حالة الحساب")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();
    }
}