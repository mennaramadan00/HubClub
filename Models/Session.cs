using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HubClub.Models.Enums;

namespace HubClub.Models
{
    
    [Table("sessions")]
    public class Session
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionId { get; set; }

        // Foreign Keys
        [Required]
        [ForeignKey("Customer")]
        public int CusId { get; set; }

        [ForeignKey("UserPackage")]
        public int? UserPackageId { get; set; }

        [ForeignKey("PricingSetting")]
        public int? PriceSettingId { get; set; }

        [Required(ErrorMessage = "وقت البداية مطلوب")]
        [Display(Name = "وقت البداية")]
        public DateTime StartTime { get; set; }

        [Display(Name = "وقت الانتهاء")]
        public DateTime? EndTime { get; set; }

        [Required]
        [Display(Name = "مغلقة")]
        public bool IsClosed { get; set; } = false;

        [Required]
        [Display(Name = "نوع الدفع")]
        public PaymentType PaymentType { get; set; } = PaymentType.PerSession;

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999.99)]
        [Display(Name = "تكلفة الوقت")]
        public decimal TotalTimePrice { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999.99)]
        [Display(Name = "تكلفة المنتجات")]
        public decimal TotalProductPrice { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999.99)]
        [Display(Name = "الإجمالي")]
        public decimal GrandTotal { get; set; } = 0;

        [Required]
        [Display(Name = "اليوم المحاسبي")]
        public DateOnly BusinessDate { get; set; }

        //[Display(Name = "التاريخ")]
        //public DateTime Date { get; set; } = DateTime.Now;

        
        [Timestamp]
        public DateTime RowVersion { get; set; }

        // Navigation properties
        public Customer Customer { get; set; } = null!;
        public UserPackage? UserPackage { get; set; }
        public PricingSetting? PricingSetting { get; set; }
        public ICollection<SessionProduct> SessionProducts { get; set; } = new List<SessionProduct>();
    }
}