using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AssetFlow.Models
{
    // Identity's stock user only carries an email and a password hash. The request
    // queue has to show the admin who asked and which department they sit in, and
    // making people retype that on every request form was never going to work.
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Your full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Department")]
        public string? Department { get; set; }
    }
}
