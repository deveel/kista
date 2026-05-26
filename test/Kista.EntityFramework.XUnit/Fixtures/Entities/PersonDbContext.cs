using Microsoft.EntityFrameworkCore;

namespace Kista.Entities {
	public class PersonDbContext : DbContext {
		public PersonDbContext(DbContextOptions<PersonDbContext> options) : base(options) {
		}

		public DbSet<DbPerson>? People { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<DbPerson>()
				.HasMany(x => x.Relationships)
				.WithOne(x => x.Person)
				.HasForeignKey(x => x.PersonId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<DbRelationship>()
				.HasOne(x => x.Person)
				.WithMany(x => x.Relationships)
				.HasForeignKey(x => x.PersonId)
				.IsRequired(false);
		}
	}
}
