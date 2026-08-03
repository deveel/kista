using Microsoft.EntityFrameworkCore;

using Kista;
using Kista.SampleApp.DomainEvents;

namespace Kista.SampleApp.DomainEvents.Data;

public class SampleDbContext : DbContext {
	public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) {
	}

	// SONAR: S2325 — a DbSet<T> property on a DbContext cannot be static;
	// this is the standard EF Core pattern.
	public DbSet<TaskItem> Tasks => Set<TaskItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<TaskItem>(e => {
			e.HasKey(x => x.Id);
			e.Property(x => x.Title).IsRequired().HasMaxLength(200);
		});
	}
}

public class TaskRepository : EntityRepository<TaskItem, string> {
	public TaskRepository(SampleDbContext context, IServiceProvider? services = null)
		: base(context, services) {
	}
}