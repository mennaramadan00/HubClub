using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubClub.Models
{
    public class Expense
    {
        [Key]
        public int ExpenseId { get; set; }

        [Required(ErrorMessage = "يرجى إدخال قيمة المصروف")]
        [Range(0.1, 1000000, ErrorMessage = "القيمة يجب أن تكون أكبر من الصفر")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "يرجى اختيار بند المصروف")]
        [Display(Name = "بند المصروف")]
        public int ExpenseCategoryId { get; set; }

        [ForeignKey("ExpenseCategoryId")]
        public ExpenseCategory ExpenseCategory { get; set; }

        [Display(Name = "التاريخ والوقت")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "يوم العمل (الوردية)")]
        [Column(TypeName = "date")]
        public DateTime BusinessDate { get; set; } 

        [Display(Name = "ملاحظات وتفاصيل")]
        [StringLength(500)]
        public string Notes { get; set; }
    }
}
