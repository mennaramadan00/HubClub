using System.ComponentModel.DataAnnotations;

namespace HubClub.Models
{
    public class ExpenseCategory
    {

        [Key]
        public int ExpenseCategoryId { get; set; }

        [Required(ErrorMessage = "يرجى إدخال اسم بند المصروفات")]
        [StringLength(100)]
        [Display(Name = "اسم البند (النوع)")]
        public string Name { get; set; }

        [Display(Name = "وصف أو ملاحظات")]
        [StringLength(255)]
        public string? Description { get; set; }
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();


    }
}