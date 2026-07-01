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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblPoint>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(Point,4326)");

        modelBuilder.Entity<TblLine>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(LineString,4326)");

        modelBuilder.Entity<TblPolygon>()
            .Property(x => x.Geometry)
            .HasColumnType("geometry(Polygon,4326)");
    }
}
