using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetFlow.Models
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }

       
        [Required(ErrorMessage = "Asset name is required")]
        [StringLength(100)]
        [Display(Name = "Asset Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Serial number is required")]
        [StringLength(50)]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;

       
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "Uncategorized";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; 

       
        [StringLength(100)]
        public string? Location { get; set; }

        
        [Display(Name = "Checked Out To")]
        [StringLength(100)]
        public string? CheckedOutToEmployee { get; set; }

        [Display(Name = "Employee Email")]
        [StringLength(100)]
        public string? EmployeeEmail { get; set; }

        [Display(Name = "Department")]
        [StringLength(50)]
        public string? EmployeeDepartment { get; set; }

        [Display(Name = "Checkout Date")]
        [DataType(DataType.Date)]
        public DateTime? CheckoutDate { get; set; }

        [Display(Name = "Expected Return")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedReturnDate { get; set; }

        [Display(Name = "Actual Return")]
        [DataType(DataType.Date)]
        public DateTime? ActualReturnDate { get; set; }

        [Display(Name = "Checkout Notes")]
        [StringLength(500)]
        public string? CheckoutNotes { get; set; }

        
        [Display(Name = "Condition Notes")]
        [StringLength(500)]
        public string? ConditionNotes { get; set; }

        [Display(Name = "Requires Maintenance")]
        public bool RequiresMaintenance { get; set; }

        [Display(Name = "Next Maintenance Due")]
        [DataType(DataType.Date)]
        public DateTime? NextMaintenanceDue { get; set; }

        // ADD THESE THREE PROPERTIES HERE:
        [Display(Name = "Last Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime? LastMaintenanceDate { get; set; }

        [Display(Name = "Maintenance Notes")]
        [StringLength(500)]
        public string? MaintenanceNotes { get; set; }

        [NotMapped]
        public bool IsMaintenanceDue
        {
            get
            {
                return RequiresMaintenance ||
                       (NextMaintenanceDue.HasValue && NextMaintenanceDue.Value < DateTime.Today);
            }
        }

        [Display(Name = "Vendor")]
        [StringLength(100)]
        public string? Vendor { get; set; }

        [Display(Name = "Warranty Expiry")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyExpiry { get; set; }

        [Display(Name = "General Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }  

      
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        
        [NotMapped]
        public bool IsOverdue
        {
            get
            {
                return Status == "CheckedOut" &&
                       ExpectedReturnDate.HasValue &&
                       ExpectedReturnDate.Value < DateTime.Today;
            }
        }

        [NotMapped]
        public bool IsWarrantyExpired
        {
            get
            {
                return WarrantyExpiry.HasValue &&
                       WarrantyExpiry.Value < DateTime.Today;
            }
        }
    }
}