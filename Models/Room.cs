using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HubClub.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "اسم الغرفة مطلوب")]
        [Display(Name = "اسم الغرفة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "متاحة للاستخدام؟")]
        public bool IsActive { get; set; } = true;

        // علاقة 1-to-Many مع جلسات الغرف
        public ICollection<RoomSession> RoomSessions { get; set; } = new List<RoomSession>();
    }
}