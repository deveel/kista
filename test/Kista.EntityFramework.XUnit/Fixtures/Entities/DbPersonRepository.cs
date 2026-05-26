using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista.Entities {
	public class DbPersonRepository : EntityRepository<DbPerson, Guid> {
		public DbPersonRepository(PersonDbContext context, IServiceProvider? services = null, ILogger<EntityRepository<DbPerson, Guid>>? logger = null) 
			: base(context, services, logger) {
		}

		public override IQueryable<DbPerson> AsQueryable() => base.Entities.Include(x => x.Relationships);

		protected override async ValueTask<DbPerson> OnEntityFoundByKeyAsync(Guid key, DbPerson entity, CancellationToken cancellationToken = default) {
			await Context.Entry(entity).Collection(x => x.Relationships).LoadAsync(cancellationToken);

			return entity;
		}

		public Task SetEmailAsync(DbPerson person, string email, CancellationToken cancellationToken = default) {
			person.Email = email;

			return Task.CompletedTask;
		}
	}
}
