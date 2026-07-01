using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Table("daily_closings")]
    public class DailyClosing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClosingId { get; set; }

        [Required]
        [Display(Name = "اليوم المحاسبي")]
        public DateOnly BusinessDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99)]
        [Display(Name = "إجمالي إيرادات الوقت")]
        public decimal TotalTimeRevenue { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99)]
        [Display(Name = "إجمالي إيرادات المنتجات")]
        public decimal TotalProductRevenue { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99)]
        [Display(Name = "إجمالي الكاش")]
        public decimal TotalCash { get; set; }

        [Display(Name = "مسار ملف الباك أب")]
        [StringLength(500)]
        public string ExcelBackupPath { get; set; }

        [Display(Name = "وقت الإنشاء")]
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}