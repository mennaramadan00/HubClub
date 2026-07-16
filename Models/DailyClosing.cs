using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    [Index(nameof(BusinessDate), IsUnique = true)]
    [Table("daily_closings")]
    public class DailyClosing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClosingId { get; set; }

        [Required]
        [Display(Name = "اليوم المحاسبي")]
        public DateOnly BusinessDate { get; set; } = HubClub.Helpers.BusinessHelper.GetBusinessDate(DateTime.Now);

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
        [Display(Name = "إجمالي إيرادات الباقات")]
        public decimal TotalPackageRevenue { get; set; } = 0m;


        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99)]
        [Display(Name = "إجمالي الكاش")]
        public decimal TotalCash { get; set; }

        [Display(Name = "مسار ملف الباك أب")]
        [StringLength(500)]
        public string? ExcelBackupPath { get; set; }

        [Display(Name = "وقت الإنشاء")]
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}