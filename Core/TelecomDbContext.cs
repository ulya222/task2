using Microsoft.EntityFrameworkCore;
using TelecomProd.Core.Entities;

namespace TelecomProd.Core;

public class TelecomDbContext : DbContext
{
    public TelecomDbContext(DbContextOptions<TelecomDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AssemblyUnit> AssemblyUnits => Set<AssemblyUnit>();
    public DbSet<BomItem> BomItems => Set<BomItem>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<QualityTest> QualityTests => Set<QualityTest>();
    public DbSet<DefectRecord> DefectRecords => Set<DefectRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Role>().ToTable("Role");
        modelBuilder.Entity<User>().ToTable("User");
        modelBuilder.Entity<Component>().ToTable("Component");
        modelBuilder.Entity<Supplier>().ToTable("Supplier");
        modelBuilder.Entity<Warehouse>().ToTable("Warehouse");
        modelBuilder.Entity<StockBalance>().ToTable("StockBalance");
        modelBuilder.Entity<StockMovement>().ToTable("StockMovement");
        modelBuilder.Entity<AssemblyUnit>().ToTable("AssemblyUnit");
        modelBuilder.Entity<BomItem>().ToTable("BomItem");
        modelBuilder.Entity<OrderStatus>().ToTable("OrderStatus");
        modelBuilder.Entity<ProductionOrder>().ToTable("ProductionOrder");
        modelBuilder.Entity<QualityTest>().ToTable("QualityTest");
        modelBuilder.Entity<DefectRecord>().ToTable("DefectRecord");
        modelBuilder.Entity<AuditLog>().ToTable("AuditLog");

        modelBuilder.Entity<User>().HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Component>().HasOne(c => c.Supplier).WithMany(s => s.Components).HasForeignKey(c => c.SupplierId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<StockBalance>().HasOne(sb => sb.Component).WithMany(c => c.StockBalances).HasForeignKey(sb => sb.ComponentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockBalance>().HasOne(sb => sb.Warehouse).WithMany(w => w.StockBalances).HasForeignKey(sb => sb.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockBalance>().HasIndex(sb => new { sb.ComponentId, sb.WarehouseId }).IsUnique();
        modelBuilder.Entity<StockMovement>().HasOne(sm => sm.Component).WithMany(c => c.StockMovements).HasForeignKey(sm => sm.ComponentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockMovement>().HasOne(sm => sm.Warehouse).WithMany(w => w.StockMovements).HasForeignKey(sm => sm.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BomItem>().HasOne(b => b.AssemblyUnit).WithMany(a => a.BomItems).HasForeignKey(b => b.AssemblyUnitId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BomItem>().HasOne(b => b.Component).WithMany(c => c.BomItems).HasForeignKey(b => b.ComponentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BomItem>().HasIndex(b => new { b.AssemblyUnitId, b.ComponentId }).IsUnique();
        modelBuilder.Entity<ProductionOrder>().HasOne(o => o.AssemblyUnit).WithMany(a => a.ProductionOrders).HasForeignKey(o => o.AssemblyUnitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrder>().HasOne(o => o.Status).WithMany(s => s.ProductionOrders).HasForeignKey(o => o.StatusId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrder>().HasOne(o => o.User).WithMany(u => u.ProductionOrders).HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<QualityTest>().HasOne(q => q.ProductionOrder).WithMany(o => o.QualityTests).HasForeignKey(q => q.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DefectRecord>().HasOne(d => d.ProductionOrder).WithMany(o => o.DefectRecords).HasForeignKey(d => d.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();
        modelBuilder.Entity<Component>().HasIndex(c => c.Code).IsUnique();
    }
}
