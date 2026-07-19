using System;
using System.ComponentModel.DataAnnotations;

namespace HubClub.Models
{
    public class Note
    {
        [Key]
        public int NoteId { get; set; }

        // 🟢 العنوان بقى اختياري (Nullable)، والقيمة الافتراضية بتاعته هي تاريخ اليوم
        [StringLength(100, ErrorMessage = "العنوان يجب ألا يتجاوز 100 حرف")]
        [Display(Name = "العنوان")]
        public string? Title { get; set; } = DateTime.Now.ToString("yyyy/MM/dd");

        [Required(ErrorMessage = "محتوى الملاحظة مطلوب")]
        [Display(Name = "الملاحظة")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}