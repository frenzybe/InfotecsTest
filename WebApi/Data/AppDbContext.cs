using Microsoft.EntityFrameworkCore;
using WebApi.Models;

namespace WebApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ValueRecord> Values => Set<ValueRecord>();
    public DbSet<FileResult> Results => Set<FileResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValueRecord>().HasIndex(v => v.FileName);
        modelBuilder.Entity<ValueRecord>().HasIndex(v => v.Date);
        modelBuilder.Entity<FileResult>().HasIndex(r => r.FileName);
    }
}