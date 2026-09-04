using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetFlow.Models
{
    // An employee asking for a piece of equipment. Sits between "Available" and the
    // actual checkout - the admin still owns the decision, this just queues it up.
    public class AssetRequest
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Asset")]
        public int AssetId { get; set; }

        public Asset? Asset { get; set; }

        [Required]
        [StringLength(450)]
        public string RequesterId { get; set; } = string.Empty;

        // Name, email and department are copied off the account at submit time.
        // If someone moves department later the old requests still read correctly.
        [Required]
        [StringLength(100)]
        [Display(Name = "Requested By")]
        public string RequesterName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Email")]
        public string? RequesterEmail { get; set; }

        [StringLength(50)]
        [Display(Name = "Department")]
        public string? RequesterDepartment { get; set; }

        [Required(ErrorMessage = "Say when you need the equipment from")]
        [DataType(DataType.Date)]
        [Display(Name = "Needed From")]
        public DateTime NeededFrom { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "A return date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Return By")]
        public DateTime NeededUntil { get; set; } = DateTime.Today.AddDays(7);

        [Required(ErrorMessage = "Please say what you need it for")]
        [StringLength(500)]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Submitted")]
        public DateTime RequestedOn { get; set; } = DateTime.Now;

        [Display(Name = "Reviewed")]
        public DateTime? ReviewedOn { get; set; }

        [StringLength(100)]
        [Display(Name = "Reviewed By")]
        public string? ReviewedBy { get; set; }

        [Display(Name = "Decision Notes")]
        [StringLength(500)]
        public string? ReviewNotes { get; set; }

        [NotMapped]
        public int DaysRequested
        {
            get
            {
                return (NeededUntil.Date - NeededFrom.Date).Days + 1;
            }
        }

        // Pending requests are the only ones the employee can still pull back.
        [NotMapped]
        public bool CanBeCancelled
        {
            get
            {
                return Status == "Pending";
            }
        }
    }
}
