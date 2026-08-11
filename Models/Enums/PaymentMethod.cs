using System.ComponentModel.DataAnnotations;

namespace HubClub.Models.Enums
{
    public enum PaymentMethod
    {
        [Display(Name = "كاش")]
        Cash = 0,

        [Display(Name = "إنستاباي")]
        InstaPay = 1
    }
}