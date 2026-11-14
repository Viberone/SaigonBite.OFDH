using Microsoft.EntityFrameworkCore;
using SaigonBite.OFDH.Domain;
using System.Reflection.Emit;

namespace SaigonBite.OFDH.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Voucher> Vouchers => Set<Voucher>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Voucher>(e =>
        {
            e.Property(v => v.Type).HasConversion<string>().HasMaxLength(12);
            e.Property(v => v.Status).HasConversion<string>().HasMaxLength(12);
            e.Property(v => v.Value).HasPrecision(18, 2);
            e.Property(v => v.MaxDiscount).HasPrecision(18, 2);
            e.Property(v => v.MinOrderValue).HasPrecision(18, 2);
        });
    }
}