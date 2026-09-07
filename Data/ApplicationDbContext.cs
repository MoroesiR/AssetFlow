using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Models;

namespace AssetFlow.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetRequest> AssetRequests { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<CheckoutRecord> CheckoutRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Removing an asset takes its request history with it. A request pointing
            // at equipment that no longer exists isn't worth keeping.
            builder.Entity<AssetRequest>()
                .HasOne(r => r.Asset)
                .WithMany()
                .HasForeignKey(r => r.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // The admin queue always filters on status and "my requests" on the user,
            // so both columns get an index.
            builder.Entity<AssetRequest>()
                .HasIndex(r => r.Status);

            builder.Entity<AssetRequest>()
                .HasIndex(r => r.RequesterId);

            // The bell badge counts one user's unread rows on every page load, so that
            // is the pair worth indexing.
            builder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            // The overdue sweep looks a notice up by its key before writing it.
            builder.Entity<Notification>()
                .HasIndex(n => n.SourceKey);

            // Deleting an asset takes its checkout episodes with it, same as its
            // requests. Usage figures for equipment that no longer exists are noise.
            builder.Entity<CheckoutRecord>()
                .HasOne(r => r.Asset)
                .WithMany()
                .HasForeignKey(r => r.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Closing an episode looks up the open one for an asset, and every usage
            // report groups by asset, so this is the pair that earns an index.
            builder.Entity<CheckoutRecord>()
                .HasIndex(r => new { r.AssetId, r.ReturnedOn });

            // The trend reports slice by when things went out.
            builder.Entity<CheckoutRecord>()
                .HasIndex(r => r.CheckedOutOn);

            builder.Entity<CheckoutRecord>()
                .HasIndex(r => r.Department);
        }
    }
}
