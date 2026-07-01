using Microsoft.EntityFrameworkCore;

namespace Kista.Entities {
	public class SoftDeletablePersonDbContext : DbContext {
		public SoftDeletablePersonDbContext(DbContextOptions<SoftDeletablePersonDbContext> options) : base(options) {
		}

		public DbSet<SoftDeletableDbPerson>? People { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<SoftDeletableDbPerson>()
				.HasMany(x => x.Relationships)
				.WithOne(x => x.Person)
				.HasForeignKey(x => x.PersonId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<SoftDeletableDbRelationship>()
				.HasOne(x => x.Person)
				.WithMany(x => x.Relationships)
				.HasForeignKey(x => x.PersonId)
				.IsRequired(false);

			modelBuilder.HasSoftDeleteFilter();
		}
	}
}