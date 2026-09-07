using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetFlow.Models
{
    // One episode of an asset being out with somebody, from handover to return.
    //
    // This exists because checking an asset in wiped the checkout off the asset row -
    // the holder and the checkout date were set back to null, so the moment equipment
    // came back there was no record it had ever gone out. The "checkout history"
    // report was reading those same fields, which is why it only ever showed things
    // that were still out. Anything asking how often an item gets used, how long it
    // sits idle, or what a month looked like needs the episodes kept.
    public class CheckoutRecord
    {
        [Key]
        public int Id { get; set; }

        public int AssetId { get; set; }

        public Asset? Asset { get; set; }

        // Set when the checkout came from an approved request rather than the admin
        // doing it by hand. Null for a manual checkout.
        public int? AssetRequestId { get; set; }

        // Copied rather than joined to the user, the same way AssetRequest does it, so
        // the record still reads correctly after somebody leaves or changes department.
        [Required]
        [StringLength(100)]
        [Display(Name = "Held By")]
        public string EmployeeName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EmployeeEmail { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        [Display(Name = "Checked Out")]
        public DateTime CheckedOutOn { get; set; } = DateTime.Now;

        [Display(Name = "Due Back")]
        public DateTime? DueOn { get; set; }

        // Null while the asset is still out. This is what separates an open episode
        // from a closed one, and what every utilisation figure counts.
        [Display(Name = "Returned")]
        public DateTime? ReturnedOn { get; set; }

        [StringLength(500)]
        public string? CheckoutNotes { get; set; }

        [StringLength(500)]
        public string? ConditionOnReturn { get; set; }

        // "Request" or "Manual" - worth knowing how much of the traffic comes through
        // the self-service queue versus the IT desk doing it directly.
        [Required]
        [StringLength(20)]
        public string Source { get; set; } = "Manual";

        [NotMapped]
        public bool IsOpen => ReturnedOn == null;

        // An open episode counts up to today, so a laptop that has been out three
        // weeks and is still out reports three weeks rather than nothing.
        [NotMapped]
        public int DaysHeld
        {
            get
            {
                var end = ReturnedOn?.Date ?? DateTime.Today;
                var days = (end - CheckedOutOn.Date).Days;
                return days < 0 ? 0 : days;
            }
        }

        [NotMapped]
        public bool ReturnedLate
        {
            get
            {
                return ReturnedOn.HasValue
                       && DueOn.HasValue
                       && ReturnedOn.Value.Date > DueOn.Value.Date;
            }
        }

        [NotMapped]
        public int? DaysLate
        {
            get
            {
                if (!ReturnedLate)
                {
                    return null;
                }

                return (ReturnedOn!.Value.Date - DueOn!.Value.Date).Days;
            }
        }
    }
}
