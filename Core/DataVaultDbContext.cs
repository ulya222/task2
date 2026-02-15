using Microsoft.EntityFrameworkCore;
using DataVault.Core.Entities;

namespace DataVault.Core;

public class DataVaultDbContext : DbContext
{
    public DataVaultDbContext(DbContextOptions<DataVaultDbContext> options) : base(options) { }

    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Storage> Storages => Set<Storage>();
    public DbSet<ResourceBalance> ResourceBalances => Set<ResourceBalance>();
    public DbSet<ResourceTransaction> ResourceTransactions => Set<ResourceTransaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryItem> CategoryItems => Set<CategoryItem>();
    public DbSet<TaskPhase> TaskPhases => Set<TaskPhase>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<Verification> Verifications => Set<Verification>();
    public DbSet<Remark> Remarks => Set<Remark>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.Entity<AppRole>().ToTable("AppRole");
        mb.Entity<AppUser>().ToTable("AppUser");
        mb.Entity<Resource>().ToTable("Resource");
        mb.Entity<Vendor>().ToTable("Vendor");
        mb.Entity<Vendor>().Property(v => v.ReliabilityRating).HasPrecision(5, 2);
        mb.Entity<Storage>().ToTable("Storage");
        mb.Entity<ResourceBalance>().ToTable("ResourceBalance");
        mb.Entity<ResourceTransaction>().ToTable("ResourceTransaction");
        mb.Entity<Category>().ToTable("Category");
        mb.Entity<CategoryItem>().ToTable("CategoryItem");
        mb.Entity<TaskPhase>().ToTable("TaskPhase");
        mb.Entity<WorkTask>().ToTable("WorkTask");
        mb.Entity<WorkTask>().Property(w => w.UnitCost).HasPrecision(18, 2);
        mb.Entity<Verification>().ToTable("Verification");
        mb.Entity<Remark>().ToTable("Remark");
        mb.Entity<ActivityLog>().ToTable("ActivityLog");

        mb.Entity<AppUser>().HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Resource>().HasOne(r => r.Vendor).WithMany(v => v.Resources).HasForeignKey(r => r.VendorId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<ResourceBalance>().HasOne(rb => rb.Resource).WithMany(r => r.ResourceBalances).HasForeignKey(rb => rb.ResourceId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ResourceBalance>().HasOne(rb => rb.Storage).WithMany(s => s.ResourceBalances).HasForeignKey(rb => rb.StorageId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ResourceBalance>().HasIndex(rb => new { rb.ResourceId, rb.StorageId }).IsUnique();
        mb.Entity<ResourceTransaction>().HasOne(rt => rt.Resource).WithMany(r => r.ResourceTransactions).HasForeignKey(rt => rt.ResourceId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ResourceTransaction>().HasOne(rt => rt.Storage).WithMany(s => s.ResourceTransactions).HasForeignKey(rt => rt.StorageId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<CategoryItem>().HasOne(ci => ci.Category).WithMany(c => c.CategoryItems).HasForeignKey(ci => ci.CategoryId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<CategoryItem>().HasOne(ci => ci.Resource).WithMany(r => r.CategoryItems).HasForeignKey(ci => ci.ResourceId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<CategoryItem>().HasIndex(ci => new { ci.CategoryId, ci.ResourceId }).IsUnique();
        mb.Entity<WorkTask>().HasOne(wt => wt.Category).WithMany(c => c.WorkTasks).HasForeignKey(wt => wt.CategoryId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<WorkTask>().HasOne(wt => wt.Phase).WithMany(p => p.WorkTasks).HasForeignKey(wt => wt.PhaseId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<WorkTask>().HasOne(wt => wt.User).WithMany(u => u.WorkTasks).HasForeignKey(wt => wt.UserId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<Verification>().HasOne(v => v.WorkTask).WithMany(wt => wt.Verifications).HasForeignKey(v => v.WorkTaskId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Remark>().HasOne(r => r.WorkTask).WithMany(wt => wt.Remarks).HasForeignKey(r => r.WorkTaskId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<AppUser>().HasIndex(u => u.Login).IsUnique();
        mb.Entity<Resource>().HasIndex(r => r.Code).IsUnique();
    }
}
