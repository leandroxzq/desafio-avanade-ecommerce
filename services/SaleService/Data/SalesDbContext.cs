using Microsoft.EntityFrameworkCore;
using SalesService.Models;

namespace SalesService.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SaleHistory> SaleHistories => Set<SaleHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Sale>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.SaleId);
        b.Entity<Sale>().HasMany(x => x.History).WithOne().HasForeignKey(x => x.SaleId);

        b.Entity<SaleItem>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        b.Entity<Sale>().Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
    }
}
