using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetFlow.Models
{
    // A message aimed at one person, shown in the bell menu. Phase 2 originally called
    // for email here, but there is no mail gateway behind this app and a portfolio
    // project should not depend on somebody else's free tier staying free.
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // The Identity user this is addressed to. Notifications are never broadcast -
        // an admin-wide notice is written once per admin so read state stays personal.
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        // Where clicking the notification takes you. Stored as a path rather than a
        // route name so the generating code decides, not the view.
        [StringLength(200)]
        public string? Link { get; set; }

        // Drives the icon and colour. "Request", "Decision", "Overdue", "Maintenance".
        [Required]
        [StringLength(20)]
        public string Category { get; set; } = "Request";

        public bool IsRead { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? ReadOn { get; set; }

        // Identifies the thing being reported on, e.g. "overdue-asset-14". The sweep
        // that raises overdue and maintenance notices runs every time somebody opens
        // the list, so without this it would add the same row over and over.
        [StringLength(100)]
        public string? SourceKey { get; set; }

        [NotMapped]
        public string Age
        {
            get
            {
                var span = DateTime.Now - CreatedOn;

                if (span.TotalMinutes < 1)
                {
                    return "just now";
                }

                if (span.TotalHours < 1)
                {
                    return $"{(int)span.TotalMinutes} min ago";
                }

                if (span.TotalDays < 1)
                {
                    return $"{(int)span.TotalHours} hr ago";
                }

                if (span.TotalDays < 30)
                {
                    return $"{(int)span.TotalDays} day(s) ago";
                }

                return CreatedOn.ToString("dd MMM yyyy");
            }
        }
    }
}
