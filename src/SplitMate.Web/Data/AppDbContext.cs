using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Models;

namespace SplitMate.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionShare> TransactionShares => Set<TransactionShare>();
    public DbSet<ShareStatusLog> ShareStatusLogs => Set<ShareStatusLog>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(50);
            e.HasOne(u => u.Group).WithMany(g => g.Members).HasForeignKey(u => u.GroupId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Group>(e =>
        {
            e.Property(g => g.Name).HasMaxLength(50);
            e.Property(g => g.JoinCode).HasMaxLength(8);
            e.HasIndex(g => g.JoinCode).IsUnique();
        });

        builder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Reason).HasMaxLength(30);
            e.Property(t => t.Note).HasMaxLength(100);
            e.HasIndex(t => new { t.GroupId, t.Date });
            e.HasOne<Group>().WithMany().HasForeignKey(t => t.GroupId);
            e.HasOne(t => t.Payer).WithMany().HasForeignKey(t => t.PayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(t => t.Shares).WithOne(s => s.Transaction).HasForeignKey(s => s.TransactionId);
        });

        builder.Entity<TransactionShare>(e =>
        {
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(s => s.Logs).WithOne().HasForeignKey(l => l.TransactionShareId);
        });

        builder.Entity<ShareStatusLog>(e =>
        {
            e.HasOne(l => l.ChangedBy).WithMany().HasForeignKey(l => l.ChangedById).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
