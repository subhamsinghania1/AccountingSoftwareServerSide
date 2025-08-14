using AccountingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingAPI.Data
{
    public class AccountingContext : DbContext
    {
        public AccountingContext(DbContextOptions<AccountingContext> options)
            : base(options)
        {
        }

        public DbSet<Vendor> Vendors { get; set; } = default!;
        public DbSet<LedgerEntry> LedgerEntries { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Vendor>()
                .HasMany(v => v.LedgerEntries)
                .WithOne(e => e.Vendor!)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed initial user (admin) if needed
            // In production, use a proper user management system
        }
    }
}