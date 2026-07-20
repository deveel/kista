using Kista.SampleApp.OperationPipeline.Models;

using Microsoft.EntityFrameworkCore;

namespace Kista.SampleApp.OperationPipeline.Data;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    // SONAR: S2325 — a DbSet<T> property on a DbContext cannot be static;
    // the analyzer flags this as a false positive. Suppressed intentionally.
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        });
    }
}