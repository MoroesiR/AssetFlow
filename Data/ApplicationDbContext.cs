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
        }
    }
}
