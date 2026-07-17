using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<TblPoint> TblPoints { get; set; }

    public DbSet<TblLine> TblLines { get; set; }

    public DbSet<TblPolygon> TblPolygons { get; set; }

    public DbSet<TblRole> TblRoles { get; set; }

    public DbSet<TblPermission> TblPermissions { get; set; }

    public DbSet<TblRolePermission> TblRolePermissions { get; set; }

    public DbSet<TblUserPermission> TblUserPermissions { get; set; }

    public DbSet<TblUserGeographicPermission> TblUserGeographicPermissions { get; set; }

    public DbSet<TblRoleGeographicPermission> TblRoleGeographicPermissions { get; set; }
    public DbSet<TblPoiCategory> TblPoiCategories { get; set; }
    public DbSet<TblPoi> TblPois { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblRole>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<TblPermission>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<TblPermission>()
            .HasData(
                new TblPermission { Id = 1, Name = "Point ekleme yetkisi", Code = "Point.Create", Description = "Point ekleme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 2, Name = "Point güncelleme yetkisi", Code = "Point.Update", Description = "Point güncelleme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 3, Name = "Point silme yetkisi", Code = "Point.Delete", Description = "Point silme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 4, Name = "Line ekleme yetkisi", Code = "Line.Create", Description = "Line ekleme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 5, Name = "Line güncelleme yetkisi", Code = "Line.Update", Description = "Line güncelleme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 6, Name = "Line silme yetkisi", Code = "Line.Delete", Description = "Line silme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 7, Name = "Polygon ekleme yetkisi", Code = "Polygon.Create", Description = "Polygon ekleme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 8, Name = "Polygon güncelleme yetkisi", Code = "Polygon.Update", Description = "Polygon güncelleme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 9, Name = "Polygon silme yetkisi", Code = "Polygon.Delete", Description = "Polygon silme yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 10, Name = "Envanter analizi yetkisi", Code = "Inventory.Analyze", Description = "Envanter analizi yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 11, Name = "Kullanıcı yönetimi yetkisi", Code = "User.Manage", Description = "Kullanıcı yönetimi yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 12, Name = "Rol yönetimi yetkisi", Code = "Role.Manage", Description = "Rol yönetimi yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 13, Name = "Yetki yönetimi yetkisi", Code = "Permission.Manage", Description = "Yetki yönetimi yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate },
                new TblPermission { Id = 14, Name = "Isı Haritası Analizi", Code = "Heatmap.Analyze", Description = "GeoServer üzerinden ısı haritası analizi yapma yetkisi", CreatedDate = seedDate, ModifiedDate = seedDate }
            );

        modelBuilder.Entity<TblRolePermission>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblRolePermission>()
            .HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblRolePermission>()
            .HasIndex(x => new { x.RoleId, x.PermissionId })
            .IsUnique();

        modelBuilder.Entity<TblUserPermission>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblUserPermission>()
            .HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblUserPermission>()
            .HasIndex(x => new { x.UserId, x.PermissionId })
            .IsUnique();

        modelBuilder.Entity<TblUserGeographicPermission>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(Polygon,4326)");

        modelBuilder.Entity<TblUserGeographicPermission>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblUserGeographicPermission>()
            .HasIndex(x => x.UserId);

        modelBuilder.Entity<TblRoleGeographicPermission>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(Polygon,4326)");

        modelBuilder.Entity<TblRoleGeographicPermission>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblRoleGeographicPermission>()
            .HasIndex(x => x.RoleId);

        modelBuilder.Entity<TblPoint>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(Point,4326)");

        modelBuilder.Entity<TblPoint>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblPoint>()
            .HasOne(x => x.ModifiedUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblLine>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(LineString,4326)");

        modelBuilder.Entity<TblLine>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblLine>()
            .HasOne(x => x.ModifiedUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblPolygon>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(Polygon,4326)");

        modelBuilder.Entity<TblPolygon>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblPolygon>()
            .HasOne(x => x.ModifiedUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblPoiCategory>().HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TblPoiCategory>().HasIndex(x => new { x.ParentId, x.Name });
        modelBuilder.Entity<TblPoiCategory>().HasOne(x => x.CreatedUser).WithMany().HasForeignKey(x => x.CreatedUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TblPoiCategory>().HasOne(x => x.ModifiedUser).WithMany().HasForeignKey(x => x.ModifiedUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TblPoi>().Property(x => x.Geometry).HasColumnType("geometry(Point,4326)");
        modelBuilder.Entity<TblPoi>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TblPoi>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TblPoi>().HasOne(x => x.ModifiedUser).WithMany().HasForeignKey(x => x.ModifiedUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TblPoi>().HasIndex(x => x.CategoryId);
        modelBuilder.Entity<TblPoi>().HasIndex(x => x.UserId);
    }
}
