using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Finbuckle.MultiTenant.EntityFrameworkCore;
#if !NET8_0 && !NET9_0
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
#endif
using Microsoft.EntityFrameworkCore;

namespace Kista.Entities
{
	public class PersonTenantDbContext : MultiTenantDbContext
	{
		public PersonTenantDbContext(IMultiTenantContextAccessor multiTenantContextAccessor, DbContextOptions options) 
			: base(multiTenantContextAccessor, options)
		{
		}

		public virtual DbSet<DbTenantPerson>? Persons { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<DbTenantPerson>()
				.HasMany(x => x.Relationships)
				.WithOne(x => x.Person)
				.HasForeignKey(x => x.PersonId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<DbTenantPerson>()
				.IsMultiTenant();

			modelBuilder.Entity<DbRelationship>()
				.HasOne(x => x.Person)
				.WithMany(x => x.Relationships)
				.HasForeignKey(x => x.PersonId)
				.IsRequired(false);
		}
	}
}
